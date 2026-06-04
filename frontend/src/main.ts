import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { LoginComponent } from './app/components/login.component';
import { AdesaoComponent } from './app/components/adesao.component';
import { DashboardClienteComponent } from './app/components/dashboard-cliente.component';
import { AdminPanelComponent } from './app/components/admin-panel.component';
import { AuthGuard } from './app/guards/auth.guard';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';


const routes: Routes = [
  // Rota pública - Login
  { path: 'login', component: LoginComponent },
  
  // Rota pública - Adesão (cadastro de novo cliente)
  { path: 'adesao', component: AdesaoComponent },
  
  // Rotas protegidas
  { path: 'dashboard', component: DashboardClienteComponent, canActivate: [AuthGuard] },
  { path: 'admin', component: AdminPanelComponent, canActivate: [AuthGuard] },
  
  // Rota padrão - redireciona para login se não autenticado
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  
  // Rota catch-all - redireciona para login
  { path: '**', redirectTo: 'login' }
];


bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
}).catch(err => console.error(err));