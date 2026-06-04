import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-adesao',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-green-600 to-blue-600 flex items-center justify-center p-4">
      <div class="bg-white rounded-xl shadow-2xl p-8 w-full max-w-md">
        <!-- Logo -->
        <div class="flex justify-center mb-8">
          <div class="w-16 h-16 bg-gradient-to-br from-blue-600 to-orange-600 rounded-lg flex items-center justify-center">
            <span class="text-white font-bold text-3xl">Ι</span>
          </div>
        </div>

        <!-- Title -->
        <h1 class="text-3xl font-bold text-center text-gray-900 mb-2">Compra Programada de Ações</h1>
        <p class="text-center text-gray-600 mb-8">Cadastre-se e comece a investir</p>

        <!-- Form -->
        <form (ngSubmit)="onAdesao()" class="space-y-5">
          <!-- Nome Input -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Nome Completo</label>
            <input
              type="text"
              [(ngModel)]="adesao.nome"
              name="nome"
              placeholder="João Silva"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-green-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="nomeError">{{ nomeError }}</span>
          </div>

          <!-- CPF Input -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">CPF</label>
            <input
              type="text"
              [(ngModel)]="adesao.cpf"
              name="cpf"
              placeholder="000.000.000-00"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-green-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="cpfError">{{ cpfError }}</span>
          </div>

          <!-- Email Input -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Email</label>
            <input
              type="email"
              [(ngModel)]="adesao.email"
              name="email"
              placeholder="seu@email.com"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-green-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="emailError">{{ emailError }}</span>
          </div>

          <!-- Senha Input -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Senha</label>
            <input
              type="password"
              [(ngModel)]="adesao.senha"
              name="senha"
              placeholder="Digite uma senha segura"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-green-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="senhaError">{{ senhaError }}</span>
          </div>

          <!-- Valor Mensal Input -->
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Valor Mensal de Aporte (R$)</label>
            <input
              type="number"
              [(ngModel)]="adesao.valorMensal"
              name="valorMensal"
              placeholder="3000.00"
              min="100"
              step="0.01"
              class="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:outline-none focus:border-green-600 transition"
              [disabled]="isLoading"
            />
            <span class="text-red-500 text-sm" *ngIf="valorError">{{ valorError }}</span>
            <p class="text-gray-500 text-xs mt-1">Será dividido em 3 parcelas (5º, 15º e 25º de cada mês)</p>
          </div>

          <!-- Error Message -->
          <div *ngIf="errorMessage" class="bg-red-50 border-l-4 border-red-500 p-4 rounded">
            <p class="text-red-700 text-sm">{{ errorMessage }}</p>
          </div>

          <!-- Success Message -->
          <div *ngIf="successMessage" class="bg-green-50 border-l-4 border-green-500 p-4 rounded">
            <p class="text-green-700 text-sm">{{ successMessage }}</p>
          </div>

          <!-- Submit Button -->
          <button
            type="submit"
            [disabled]="isLoading"
            class="w-full bg-gradient-to-r from-green-600 to-blue-600 text-white font-bold py-3 rounded-lg hover:shadow-lg transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span *ngIf="!isLoading">Aderir Agora</span>
            <span *ngIf="isLoading">Processando...</span>
          </button>
        </form>

        <!-- Login Link -->
        <p class="text-center text-gray-600 text-sm mt-6">
          Já tem cadastro? 
          <a routerLink="/login" class="text-green-600 font-semibold hover:underline">Faça login</a>
        </p>

        <!-- Footer -->
        <p class="text-center text-gray-600 text-xs mt-8">
          © 2026 Compra Programada de Ações - Todos os direitos reservados
        </p>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }
  `]
})
export class AdesaoComponent implements OnInit {
  adesao = {
    nome: '',
    cpf: '',
    email: '',
    senha: '',
    valorMensal: 0
  };

  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  nomeError: string = '';
  cpfError: string = '';
  emailError: string = '';
  senhaError: string = '';
  valorError: string = '';

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Se já está logado, redireciona para dashboard
    // (você pode adicionar verificação aqui se necessário)
  }

  onAdesao(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.nomeError = '';
    this.cpfError = '';
    this.emailError = '';
    this.senhaError = '';
    this.valorError = '';

    // Validações
    if (!this.adesao.nome.trim()) {
      this.nomeError = 'Nome é obrigatório';
      return;
    }

    if (!this.adesao.cpf.trim()) {
      this.cpfError = 'CPF é obrigatório';
      return;
    }

    const cpfLimpo = this.adesao.cpf.replace(/\D/g, '');
    if (cpfLimpo.length !== 11) {
      this.cpfError = 'CPF deve ter 11 dígitos';
      return;
    }

    if (!this.adesao.email.trim()) {
      this.emailError = 'Email é obrigatório';
      return;
    }

    if (!this.adesao.email.includes('@')) {
      this.emailError = 'Email inválido';
      return;
    }

    if (!this.adesao.senha.trim()) {
      this.senhaError = 'Senha é obrigatória';
      return;
    }

    if (this.adesao.senha.length < 8) {
      this.senhaError = 'Senha deve ter no mínimo 8 caracteres';
      return;
    }

    if (!this.adesao.valorMensal || this.adesao.valorMensal < 100) {
      this.valorError = 'Valor mensal deve ser no mínimo R$ 100';
      return;
    }

    this.isLoading = true;

    // Chamar API para criar novo cliente
    this.apiService.aderirProduto({
      nome: this.adesao.nome,
      cpf: cpfLimpo,
      email: this.adesao.email,
      senha: this.adesao.senha,
      valorMensal: this.adesao.valorMensal
    }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.successMessage = 'Cadastro realizado com sucesso! Redirecionando para login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error) => {
        this.isLoading = false;
        if (error.status === 400) {
          this.errorMessage = error.error?.erro || 'Dados inválidos';
        } else if (error.status === 409) {
          this.errorMessage = 'CPF ou email já cadastrado';
        } else if (error.status === 0) {
          this.errorMessage = 'Erro de conexão com o servidor';
        } else {
          this.errorMessage = error.error?.erro || 'Erro ao cadastrar';
        }
      }
    });
  }
}
