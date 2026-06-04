import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-orange-50 to-red-100 p-8">
      <div class="max-w-6xl mx-auto">
        <!-- Header -->
        <div class="mb-8">
          <h1 class="text-4xl font-bold text-gray-900 mb-2">Painel Administrativo</h1>
          <p class="text-gray-600">Gerencie a cesta de recomendação e o motor de compra</p>
        </div>

        <!-- Cadastrar Cesta -->
        <div class="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Cadastrar Cesta Top Five</h2>
          <div class="mb-4">
            <input 
              [(ngModel)]="novaCesta.nome" 
              placeholder="Nome da Cesta" 
              class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
            />
          </div>

          <!-- Itens da Cesta -->
          <div class="space-y-3 mb-4">
            <div *ngFor="let item of novaCesta.itens; let i = index" class="flex gap-2">
              <input 
                [(ngModel)]="item.ticker" 
                placeholder="Ticker (ex: PETR4)" 
                class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
              />
              <input 
                [(ngModel)]="item.percentual" 
                placeholder="Percentual" 
                type="number"
                class="w-24 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
              />
              <button 
                (click)="removerItem(i)"
                class="bg-red-500 hover:bg-red-600 text-white px-3 py-2 rounded-lg"
              >
                Remover
              </button>
            </div>
          </div>

          <button 
            (click)="adicionarItem()"
            class="bg-gray-500 hover:bg-gray-600 text-white font-semibold py-2 px-4 rounded-lg mb-4"
          >
            + Adicionar Ativo
          </button>

          <button 
            (click)="cadastrarCesta()"
            class="w-full bg-orange-600 hover:bg-orange-700 text-white font-semibold py-3 rounded-lg transition duration-200"
          >
            Cadastrar Cesta
          </button>
          <p *ngIf="mensagemCesta" class="mt-4 text-green-600">{{ mensagemCesta }}</p>
        </div>

        <!-- Cesta Atual -->
        <div class="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Cesta Atual</h2>
          <button 
            (click)="consultarCestaAtual()"
            class="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-2 px-6 rounded-lg mb-4"
          >
            Consultar Cesta
          </button>

          <div *ngIf="cestaAtual" class="bg-gradient-to-r from-orange-500 to-red-600 text-white rounded-lg p-6">
            <h3 class="text-xl font-semibold mb-4">Cesta Top Five (ativa)</h3>
            <div class="grid grid-cols-5 gap-4">
              <div *ngFor="let item of cestaAtual.ativos" class="text-center">
                <p class="text-orange-100">{{ item.ticker }}</p>
                <p class="text-2xl font-bold">{{ item.percentual }}%</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Executar Motor de Compra -->
        <div class="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Motor de Compra Programada</h2>
          <div class="flex gap-4 mb-4">
            <input 
              [(ngModel)]="dataExecucao" 
              type="date"
              class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
            />
            <button 
              (click)="executarCompra()"
              class="bg-green-600 hover:bg-green-700 text-white font-semibold py-2 px-8 rounded-lg transition duration-200"
            >
              Executar Compra
            </button>
          </div>
          <p *ngIf="mensagemMotor" class="text-green-600 font-semibold">{{ mensagemMotor }}</p>
        </div>

        <!-- Custodia Master -->
        <div class="bg-white rounded-lg shadow-lg p-6">
          <h2 class="text-2xl font-semibold text-gray-800 mb-4">Custódia Master</h2>
          <button 
            (click)="consultarCustodiaMaster()"
            class="bg-purple-600 hover:bg-purple-700 text-white font-semibold py-2 px-6 rounded-lg mb-4"
          >
            Consultar Resíduos
          </button>

          <div *ngIf="custodiaMaster" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-100">
                <tr>
                  <th class="px-4 py-2 text-left">Ativo</th>
                  <th class="px-4 py-2 text-left">Quantidade</th>
                  <th class="px-4 py-2 text-left">Lote/Fracionário</th>
                  <th class="px-4 py-2 text-left">Preço Médio</th>
                  <th class="px-4 py-2 text-left">Valor Total</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of custodiaMaster" class="border-b hover:bg-gray-50">
                  <td class="px-4 py-2 font-semibold">{{ item.ticker }}</td>
                  <td class="px-4 py-2">{{ item.quantidade }}</td>
                  <td class="px-4 py-2">{{ item.quantidadeLotePadrao }} / {{ item.quantidadeFracionario }}</td>
                  <td class="px-4 py-2">R$ {{ item.precoMedio | number:'1.2-2' }}</td>
                  <td class="px-4 py-2 font-semibold">R$ {{ item.valorAtual | number:'1.2-2' }}</td>
                </tr>
              </tbody>
            </table>
            <div class="mt-4 text-right">
              <p class="text-lg font-semibold">Total de Resíduos: R$ {{ totalResiduos | number:'1.2-2' }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminPanelComponent implements OnInit {
  novaCesta = { nome: '', itens: [{ ticker: '', percentual: 0 }] };
  cestaAtual: any = null;
  custodiaMaster: any[] | null = null;
  totalResiduos = 0;
  dataExecucao: string = new Date().toISOString().split('T')[0];
  mensagemCesta: string = '';
  mensagemMotor: string = '';

  constructor(private apiService: ApiService) { }

  ngOnInit(): void { }

  adicionarItem(): void {
    this.novaCesta.itens.push({ ticker: '', percentual: 0 });
  }

  removerItem(index: number): void {
    this.novaCesta.itens.splice(index, 1);
  }

  cadastrarCesta(): void {
    const payload = {
      nome: this.novaCesta.nome,
      ativos: this.novaCesta.itens.map(i => ({ ticker: i.ticker, percentual: Number(i.percentual) }))
    };
    this.apiService.cadastrarCesta(payload).subscribe({
      next: (response) => {
        this.mensagemCesta = response.mensagem || 'Cesta cadastrada com sucesso!';
        this.novaCesta = { nome: '', itens: [{ ticker: '', percentual: 0 }] };
      },
      error: (error) => {
        this.mensagemCesta = `Erro: ${error.error?.erro || 'falha ao cadastrar cesta'}`;
      }
    });
  }

  consultarCestaAtual(): void {
    this.apiService.consultarCestaAtual().subscribe({
      next: (response) => {
        this.cestaAtual = response;
      },
      error: (error) => {
        console.error('Erro ao consultar cesta', error);
      }
    });
  }

  consultarCustodiaMaster(): void {
    this.apiService.consultarCustodiaMaster().subscribe({
      next: (response) => {
        this.custodiaMaster = response;
        this.totalResiduos = response.reduce((acc, x) => acc + x.valorAtual, 0);
      },
      error: (error) => {
        console.error('Erro ao consultar custódia master', error);
      }
    });
  }

  executarCompra(): void {
    this.mensagemMotor = '';
    this.apiService.executarCompra(this.dataExecucao).subscribe({
      next: (r) => {
        this.mensagemMotor = r.mensagem || 'Compra programada executada com sucesso!';
      },
      error: (error) => {
        this.mensagemMotor = `Erro: ${error.error?.erro || 'falha ao executar o motor'}`;
      }
    });
  }
}
