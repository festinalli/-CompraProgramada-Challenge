import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';



@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  template: `
    <!-- Layout com Navigation (apenas quando autenticado) -->
    <div *ngIf="isAuthenticated" class="min-h-screen bg-gray-50">
      <!-- Navigation -->
      <nav class="bg-white shadow-lg sticky top-0 z-50">
        <div class="max-w-7xl mx-auto px-8 py-4 flex justify-between items-center">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-gradient-to-br from-blue-600 to-orange-600 rounded-lg flex items-center justify-center">
              <span class="text-white font-bold text-lg">Ι</span>
            </div>
            <h1 class="text-2xl font-bold text-gray-900">Compra Programada de Ações</h1>
          </div>
          <div class="flex gap-6 items-center">
            <button 
              (click)="irParaDashboard()"
              class="text-gray-700 hover:text-blue-600 font-medium transition bg-none border-none cursor-pointer"
            >
              Dashboard
            </button>
            <button 
              (click)="irParaAdmin()"
              class="text-gray-700 hover:text-blue-600 font-medium transition bg-none border-none cursor-pointer"
            >
              Admin
            </button>
            <button 
              (click)="onLogout()"
              class="px-6 py-2 rounded-lg font-semibold bg-red-600 text-white hover:bg-red-700 transition duration-200"
            >
              Sair
            </button>
          </div>
        </div>
      </nav>


      <!-- Content -->
      <div class="py-8">
        <router-outlet></router-outlet>
      </div>


      <!-- Footer -->
      <footer class="bg-gray-900 text-gray-300 py-8 mt-16">
        <div class="max-w-6xl mx-auto px-8 text-center">
          <p>© 2026 Compra Programada de Ações - Sistema de Compra Programada de Ações</p>
          <p class="text-sm mt-2">Desenvolvido com Angular + .NET Core</p>
        </div>
      </footer>
    </div>


    <!-- Layout sem Navigation (quando não autenticado) -->
    <div *ngIf="!isAuthenticated" class="min-h-screen">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }
  `]
})
export class AppComponent implements OnInit {
  isAuthenticated: boolean = false;


  constructor(
    private authService: AuthService,
    private router: Router
  ) {}


  ngOnInit(): void {
    // Subscribe ao estado de autenticação
    this.authService.currentUser$.subscribe(user => {
      this.isAuthenticated = !!user;
      // Se não está autenticado, redireciona para login
      if (!user && !this.router.url.includes('/adesao')) {
        this.router.navigate(['/login']);
      }
    });


    // Se já tem token, mantém logado
    if (this.authService.isAuthenticated()) {
      this.isAuthenticated = true;
    }
  }


  irParaDashboard(): void {
    this.router.navigate(['/dashboard']);
  }


  irParaAdmin(): void {
    this.router.navigate(['/admin']);
  }


  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}