import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-dashboard-cliente',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 p-8">
      <div class="max-w-6xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h1 class="text-4xl font-bold text-gray-900 mb-2">Dashboard do Cliente</h1>
          <p class="text-gray-600">Acompanhe sua carteira de investimentos</p>
          <p class="text-sm text-gray-500 mt-2">Bem-vindo, {{ clienteNome }}!</p>
        </div>

        <!-- Informações do Cliente -->
        <div class="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Suas Informações</h2>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div class="bg-gradient-to-br from-blue-500 to-blue-600 text-white rounded-lg p-6">
              <p class="text-blue-100 text-sm">Valor Mensal de Aporte</p>
              <p class="text-3xl font-bold mt-2">R$ {{ valorMensalAporte | number: '1.2-2' }}</p>
              <p class="text-blue-100 text-xs mt-2">Dividido em 3 parcelas</p>
            </div>
            <div class="bg-gradient-to-br from-green-500 to-green-600 text-white rounded-lg p-6">
              <p class="text-green-100 text-sm">Próxima Compra</p>
              <p class="text-3xl font-bold mt-2">{{ proximaCompra }}</p>
              <p class="text-green-100 text-xs mt-2">Compra automática</p>
            </div>
            <div class="bg-gradient-to-br from-purple-500 to-purple-600 text-white rounded-lg p-6">
              <p class="text-purple-100 text-sm">Status</p>
              <p class="text-3xl font-bold mt-2">{{ statusCliente }}</p>
              <p class="text-purple-100 text-xs mt-2">Ativo</p>
            </div>
          </div>
        </div>

        <!-- Consultar Carteira -->
        <div class="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Sua Carteira</h2>
          
          <button 
            (click)="carregarCarteira()"
            [disabled]="carregandoCarteira"
            class="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-2 px-6 rounded-lg transition duration-200 disabled:opacity-50"
          >
            {{ carregandoCarteira ? 'Carregando...' : 'Carregar Carteira' }}
          </button>

          <!-- Resultado da Carteira -->
          <div *ngIf="carteira" class="mt-6">
            <div class="bg-gradient-to-r from-blue-500 to-indigo-600 text-white rounded-lg p-6 mb-6">
              <h3 class="text-xl font-semibold mb-4">{{ carteira.nome }}</h3>
              <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <p class="text-blue-100">Valor Investido</p>
                  <p class="text-2xl font-bold">R$ {{ carteira.valorInvestido | number:'1.2-2' }}</p>
                </div>
                <div>
                  <p class="text-blue-100">Valor Atual</p>
                  <p class="text-2xl font-bold">R$ {{ carteira.valorAtual | number:'1.2-2' }}</p>
                </div>
                <div>
                  <p class="text-blue-100">Rentabilidade</p>
                  <p class="text-2xl font-bold" [ngClass]="carteira.percentualRentabilidade >= 0 ? 'text-green-300' : 'text-red-300'">
                    {{ carteira.percentualRentabilidade | number:'1.2-2' }}%
                  </p>
                </div>
              </div>
            </div>

            <!-- Ativos -->
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-gray-100">
                  <tr>
                    <th class="px-4 py-2 text-left">Ativo</th>
                    <th class="px-4 py-2 text-left">Quantidade</th>
                    <th class="px-4 py-2 text-left">Preço Médio</th>
                    <th class="px-4 py-2 text-left">Cotação Atual</th>
                    <th class="px-4 py-2 text-left">Valor Atual</th>
                    <th class="px-4 py-2 text-left">P/L</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let ativo of carteira.posicoes" class="border-b hover:bg-gray-50">
                    <td class="px-4 py-2 font-semibold">{{ ativo.ticker }}</td>
                    <td class="px-4 py-2">{{ ativo.quantidade }}</td>
                    <td class="px-4 py-2">R$ {{ ativo.precoMedio | number:'1.2-2' }}</td>
                    <td class="px-4 py-2">R$ {{ ativo.cotacaoAtual | number:'1.2-2' }}</td>
                    <td class="px-4 py-2">R$ {{ ativo.valorAtual | number:'1.2-2' }}</td>
                    <td class="px-4 py-2" [ngClass]="ativo.rentabilidade >= 0 ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold'">
                      R$ {{ ativo.rentabilidade | number:'1.2-2' }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Mensagem de Vazio -->
          <div *ngIf="!carteira && !carregandoCarteira" class="mt-6 text-center text-gray-500">
            <p>Clique em "Carregar Carteira" para visualizar seus investimentos</p>
          </div>
        </div>

        <!-- Informações sobre Compra Programada -->
        <div class="bg-blue-50 border-l-4 border-blue-500 p-6 rounded-lg">
          <h3 class="text-lg font-semibold text-blue-900 mb-2">ℹ️ Como Funciona a Compra Programada</h3>
          <ul class="text-blue-800 text-sm space-y-2">
            <li>✓ Suas compras acontecem automaticamente nos dias <strong>5, 15 e 25</strong> de cada mês</li>
            <li>✓ Cada compra corresponde a <strong>1/3 do seu valor mensal</strong> de aporte</li>
            <li>✓ Os ativos são distribuídos conforme a <strong>Cesta Top Five</strong> (PETR4, VALE3, ITUB4, BBDC4, WEGE3)</li>
            <li>✓ Você pode acompanhar todas as compras no histórico abaixo</li>
          </ul>
        </div>
      </div>
    </div>
  `
})
export class DashboardClienteComponent implements OnInit {
  carteira: any = null;
  carregandoCarteira: boolean = false;
  clienteNome: string = '';
  valorMensalAporte: number = 0;
  proximaCompra: string = '';
  statusCliente: string = 'Ativo';

  constructor(
    private apiService: ApiService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.carregarDadosCliente();
    this.calcularProximaCompra();
  }

  carregarDadosCliente(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.clienteNome = user.nome || 'Cliente';
      this.valorMensalAporte = user.valorMensalAporte ?? 0;
    }
  }

  calcularProximaCompra(): void {
    const hoje = new Date();
    const dia = hoje.getDate();
    const mes = hoje.getMonth();
    const ano = hoje.getFullYear();

    let proximoDia = 0;

    if (dia < 5) {
      proximoDia = 5;
    } else if (dia < 15) {
      proximoDia = 15;
    } else if (dia < 25) {
      proximoDia = 25;
    } else {
      // Próximo mês, dia 5
      proximoDia = 5;
    }

    const proximaData = new Date(ano, mes, proximoDia);
    if (dia >= 25) {
      proximaData.setMonth(mes + 1);
    }

    this.proximaCompra = proximaData.toLocaleDateString('pt-BR', { 
      day: '2-digit', 
      month: '2-digit', 
      year: 'numeric' 
    });
  }

  carregarCarteira(): void {
    this.carregandoCarteira = true;
    const user = this.authService.getCurrentUser();
    
    if (user && user.clienteId) {
      this.apiService.consultarCarteira(user.clienteId).subscribe({
        next: (response) => {
          this.carteira = response;
          this.carregandoCarteira = false;
        },
        error: (error) => {
          console.error('Erro ao consultar carteira', error);
          this.carregandoCarteira = false;
        }
      });
    }
  }
}