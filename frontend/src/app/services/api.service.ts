import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdesaoRequest, AdesaoResponse, Carteira, CestaResponse, CestaHistorico,
  CustodiaMaster, MotorResultado, SaidaResponse
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  // Relativo: o nginx faz proxy de /api para a API (mesma origem, sem CORS).
  private apiUrl = '/api';

  constructor(private http: HttpClient) { }

  // ----- Clientes -----
  aderirProduto(dados: AdesaoRequest): Observable<AdesaoResponse> {
    return this.http.post<AdesaoResponse>(`${this.apiUrl}/clientes/adesao`, dados);
  }

  sairProduto(clienteId: number, motivo = ''): Observable<SaidaResponse> {
    return this.http.post<SaidaResponse>(`${this.apiUrl}/clientes/${clienteId}/saida`, { motivo });
  }

  alterarValorMensal(clienteId: number, novoValor: number): Observable<AdesaoResponse> {
    return this.http.put<AdesaoResponse>(`${this.apiUrl}/clientes/${clienteId}/valor-mensal`,
      { novoValorMensalAporte: novoValor });
  }

  consultarCarteira(clienteId: number): Observable<Carteira> {
    return this.http.get<Carteira>(`${this.apiUrl}/clientes/${clienteId}/carteira`);
  }

  consultarRentabilidade(clienteId: number): Observable<Carteira> {
    return this.http.get<Carteira>(`${this.apiUrl}/clientes/${clienteId}/rentabilidade`);
  }

  // ----- Admin - Cesta -----
  cadastrarCesta(dados: { nome: string; ativos: { ticker: string; percentual: number }[] }): Observable<CestaResponse> {
    return this.http.post<CestaResponse>(`${this.apiUrl}/admin/cesta`, dados);
  }

  consultarCestaAtual(): Observable<CestaResponse> {
    return this.http.get<CestaResponse>(`${this.apiUrl}/admin/cesta/atual`);
  }

  consultarHistoricoCestas(): Observable<CestaHistorico[]> {
    return this.http.get<CestaHistorico[]>(`${this.apiUrl}/admin/cesta/historico`);
  }

  // ----- Admin - Conta Master -----
  consultarCustodiaMaster(): Observable<CustodiaMaster[]> {
    return this.http.get<CustodiaMaster[]>(`${this.apiUrl}/admin/conta-master/custodia`);
  }

  // ----- Motor -----
  executarCompra(dataReferencia: string): Observable<MotorResultado> {
    return this.http.post<MotorResultado>(`${this.apiUrl}/motor/executar-compra`, { dataReferencia });
  }
}
