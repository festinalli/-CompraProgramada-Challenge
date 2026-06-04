using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace API.Security
{
    /// <summary>
    /// Exige uma permissão fina (recurso:ação). Uso: [HasPermission(Permissions.MotorExecutar)].
    /// O nome da permissão vira o nome da policy, resolvida dinamicamente pelo provider abaixo.
    /// </summary>
    public sealed class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permission) => Policy = permission;
    }

    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission) => Permission = permission;
    }

    /// <summary>Autoriza se o JWT contém a claim "permission" igual à exigida.</summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.Claims.Any(c => c.Type == "permission" && c.Value == requirement.Permission))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Resolve qualquer policy não-registrada como uma exigência de permissão
    /// (evita registrar manualmente uma policy por permissão).
    /// </summary>
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            => _fallback = new DefaultAuthorizationPolicyProvider(options);

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
