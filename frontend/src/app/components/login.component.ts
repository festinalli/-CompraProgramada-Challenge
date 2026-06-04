import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

type Modo = 'cliente' | 'admin';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-blue-600 to-orange-600 flex items-center justify-center">
      <div class="bg-white rounded-xl shadow-2xl p-8 w-full max-w-md">
        <!-- Logo -->
        <div class="flex justify-center mb-6">
          <div class="w-16 h-16 bg-gradient-to-br from-blue-600 to-orange-600 rounded-lg flex items-center justify-center">
            <span class="text-white font-bold text-3xl">Ι</span>
          </div>
        </div>

        <h1 class="text-3xl font-bold text-center text-gray-900 mb-2">Compra Programada de Ações</h1>
        <p class="text-center text-gray-600 mb-6">Sistema de Compra Programada de Ações</p>

        <!-- Seletor de perfil -->
        <div class="flex bg-gray-100 rounded-lg p-1 mb-6">
          <button type="button" (click)="setModo('cliente')"
            [class]="modo === 'cliente' ? 'bg-white shadow text-blue-700' : 'text-gray-500'"
            class="flex-1 py-2 rounded-md text-sm font-semibold transition">Cliente</button>
          <button type="button" (click)="setModo('admin')"
            [class]="modo === 'admin' ? 'bg-white shadow text-blue-700' : 'text-gray-500'"
            class="flex-1 py-2 rounded-md text-sm font-semibold transition">Administrador</button>
        </div>

        <form (ngSubmit)="onSubmit()" class="space-y-6">
          <!-- Identificador: CPF (cliente) ou Usuário (admin) -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">
              {{ modo === 'cliente' ? 'CPF' : 'Usuário' }}
            </label>
            <input
              type="text"
              [(ngModel)]="identificador"
              name="identificador"
              [placeholder]="modo === 'cliente' ? '000.000.000-00' : 'admin'"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-blue-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="idError">{{ idError }}</span>
          </div>

          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Senha</label>
            <input
              type="password"
              [(ngModel)]="senha"
              name="senha"
              placeholder="Digite sua senha"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-blue-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="senhaError">{{ senhaError }}</span>
          </div>

          <div *ngIf="errorMessage" class="bg-red-50 border-l-4 border-red-500 p-4 rounded">
            <p class="text-red-700 text-sm">{{ errorMessage }}</p>
          </div>
          <div *ngIf="successMessage" class="bg-green-50 border-l-4 border-green-500 p-4 rounded">
            <p class="text-green-700 text-sm">{{ successMessage }}</p>
          </div>

          <button
            type="submit"
            [disabled]="isLoading"
            class="w-full bg-gradient-to-r from-blue-600 to-orange-600 text-white font-bold py-3 rounded-lg hover:shadow-lg transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span *ngIf="!isLoading">{{ modo === 'cliente' ? 'Entrar' : 'Entrar como Admin' }}</span>
            <span *ngIf="isLoading">Carregando...</span>
          </button>

          <p class="text-center text-xs text-gray-400">
            <ng-container *ngIf="modo === 'cliente'">Investidor — acesse com CPF e senha</ng-container>
            <ng-container *ngIf="modo === 'admin'">Acesso restrito ao backoffice</ng-container>
          </p>
        </form>

        <p class="text-center text-gray-600 text-sm mt-8">
          © 2026 Compra Programada de Ações - Todos os direitos reservados
        </p>
      </div>
    </div>
  `,
  styles: [`:host { display: block; width: 100%; }`]
})
export class LoginComponent implements OnInit {
  modo: Modo = 'cliente';
  identificador = '';
  senha = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  idError = '';
  senhaError = '';

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate([this.authService.isAdmin() ? '/admin' : '/dashboard']);
    }
  }

  setModo(modo: Modo): void {
    this.modo = modo;
    this.errorMessage = this.idError = this.senhaError = '';
  }

  onSubmit(): void {
    this.errorMessage = this.successMessage = this.idError = this.senhaError = '';

    if (!this.identificador.trim()) {
      this.idError = this.modo === 'cliente' ? 'CPF é obrigatório' : 'Usuário é obrigatório';
      return;
    }
    if (!this.senha.trim()) {
      this.senhaError = 'Senha é obrigatória';
      return;
    }

    if (this.modo === 'cliente') {
      const cpf = this.identificador.replace(/\D/g, '');
      if (cpf.length !== 11) { this.idError = 'CPF deve ter 11 dígitos'; return; }
      this.run(this.authService.login(cpf, this.senha), '/dashboard', 'Login realizado com sucesso!');
    } else {
      this.run(this.authService.loginAdmin(this.identificador.trim(), this.senha), '/admin', 'Login administrativo realizado!');
    }
  }

  private run(obs: import('rxjs').Observable<unknown>, destino: string, sucesso: string): void {
    this.isLoading = true;
    obs.subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = sucesso;
        setTimeout(() => this.router.navigate([destino]), 800);
      },
      error: (error) => this.handleError(error)
    });
  }

  private handleError(error: { status?: number; error?: { erro?: string; mensagem?: string } }): void {
    this.isLoading = false;
    if (error.status === 401) {
      this.errorMessage = 'Credenciais inválidas';
    } else if (error.status === 0) {
      this.errorMessage = 'Erro de conexão com o servidor';
    } else {
      this.errorMessage = error.error?.erro || error.error?.mensagem || 'Erro ao fazer login';
    }
  }
}
