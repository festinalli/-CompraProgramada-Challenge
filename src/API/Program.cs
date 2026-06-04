using Application.Abstractions;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Infrastructure.Parsers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using FluentValidation;


var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO DE LOGGING COM SERILOG =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/corretora-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ===== CONFIGURAÇÃO DE SERVIÇOS =====

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== MEDIATR + CQRS =====
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.Features.Clientes.Commands.AderirClienteCommand).Assembly));

// ===== FLUENT VALIDATION =====
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ===== SWAGGER COM SEGURANÇA JWT =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Compra Programada de Ações — API",
        Version = "v1",
        Description = "API REST para Sistema de Compra Programada de Ações",
        Contact = new OpenApiContact
        {
            Name = "Compra Programada de Ações",
            Email = "support@corretora.local"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary"
        }
    });

    // Incluir XML comments
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "API.xml");
    if (File.Exists(xmlFile))
        c.IncludeXmlComments(xmlFile);

    // Adicionar segurança JWT ao Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ===== AUTENTICAÇÃO JWT =====
var jwtSecretKey = builder.Configuration["JWT:SecretKey"]
    ?? throw new InvalidOperationException("JWT:SecretKey não configurado");
var jwtIssuer = builder.Configuration["JWT:Issuer"]
    ?? throw new InvalidOperationException("JWT:Issuer não configurado");
var jwtAudience = builder.Configuration["JWT:Audience"]
    ?? throw new InvalidOperationException("JWT:Audience não configurado");

var key = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ===== AUTORIZAÇÃO POR PERMISSÃO (RBAC) =====
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, API.Security.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, API.Security.PermissionAuthorizationHandler>();

// ===== RATE LIMITING (anti brute-force nos endpoints de auth) =====
// Por IP (atrás de proxy, habilite ForwardedHeaders p/ usar o IP real do cliente).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0
            }));
});

// ===== BANCO DE DADOS =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
// Inversão de dependência: a Application depende de IAppDbContext (não do EF concreto).
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ===== SERVIÇOS DE INFRAESTRUTURA =====
builder.Services.AddSingleton<CotahistParser>();
builder.Services.AddSingleton<ICotahistParser>(sp => sp.GetRequiredService<CotahistParser>());
builder.Services.AddSingleton<ICotacaoProvider, CotacaoProvider>();
builder.Services.AddSingleton<IKafkaProducer>(sp =>
    new KafkaProducer(builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092"));

// ===== SERVIÇOS DE APLICAÇÃO =====
builder.Services.AddSingleton<Application.Services.Security.IPasswordHasher, Application.Services.Security.Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Serviços de negócio existentes
builder.Services.AddScoped<IMotorCompraService>(sp =>
{
    var context = sp.GetRequiredService<AppDbContext>();
    var parser = sp.GetRequiredService<CotahistParser>();
    var kafka = sp.GetRequiredService<IKafkaProducer>();
    var pastaCotacoes = builder.Configuration["Cotacoes:PastaLocal"] ?? "cotacoes";
    return new MotorCompraService(context, parser, kafka, pastaCotacoes);
});

builder.Services.AddScoped<IRebalanceamentoService>(sp =>
{
    var context = sp.GetRequiredService<AppDbContext>();
    var parser = sp.GetRequiredService<CotahistParser>();
    var kafka = sp.GetRequiredService<IKafkaProducer>();
    var logger = sp.GetRequiredService<ILogger<RebalanceamentoService>>();
    var pastaCotacoes = builder.Configuration["Cotacoes:PastaLocal"] ?? "cotacoes";
    return new RebalanceamentoService(context, parser, kafka, logger, pastaCotacoes);
});

// ===== CORS (por origem configurável) =====
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost", "http://localhost:80", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== BUILD APPLICATION =====
var app = builder.Build();

// ===== MIDDLEWARE =====
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada de Ações — API v1");
    c.RoutePrefix = "swagger"; // Swagger na raiz
});

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();

// Autenticação ANTES de Autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== INICIALIZAR BANCO DE DADOS =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated(); // testes de integração (InMemory)
    Log.Information("✅ Banco de dados pronto");

    var hasher = scope.ServiceProvider.GetRequiredService<Application.Services.Security.IPasswordHasher>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await API.Seed.DbSeeder.SeedAsync(db, hasher, builder.Configuration, seedLogger);
    Log.Information("✅ Seed concluído");
}

// ===== INICIAR CONSUMER KAFKA =====
Log.Information("✅ Consumer Kafka iniciado");

Log.Information("🚀 Aplicação iniciada com sucesso");

app.Run();

// Exposto para testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
