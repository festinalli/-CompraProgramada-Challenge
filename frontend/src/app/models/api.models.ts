// Tipagem dos contratos da API (espelha os DTOs do backend).

export interface AdesaoRequest {
  cpf: string;
  nome: string;
  email: string;
  valorMensal: number;
  senha: string;
}

export interface AdesaoResponse {
  clienteId: number;
  nome: string;
  cpfMascarado: string;
  valorMensalAporte: number;
  dataAdesao: string;
  mensagem: string;
}

export interface PosicaoAtivo {
  ticker: string;
  quantidade: number;
  precoMedio: number;
  cotacaoAtual: number;
  valorAtual: number;
  rentabilidade: number;
  percentualCarteira: number;
}

export interface Carteira {
  clienteId: number;
  nome: string;
  saldoTotal: number;
  valorInvestido: number;
  valorAtual: number;
  rentabilidade: number;
  percentualRentabilidade: number;
  posicoes: PosicaoAtivo[];
}

export interface CestaAtivo {
  ticker: string;
  percentual: number;
}

export interface CestaResponse {
  cestaId: number;
  ativa: boolean;
  dataCriacao: string;
  ativos: CestaAtivo[];
  mensagem?: string;
}

export interface CestaHistorico {
  cestaId: number;
  nome: string;
  ativa: boolean;
  dataCriacao: string;
  dataDesativacao?: string;
  ativos: CestaAtivo[];
}

export interface CustodiaMaster {
  ticker: string;
  quantidade: number;
  quantidadeLotePadrao: number;
  quantidadeFracionario: number;
  precoMedio: number;
  valorAtual: number;
}

export interface MotorResultado {
  dataExecucao: string;
  clientesProcessados: number;
  ordensCriadas: number;
  totalIR: number;
  sucesso: boolean;
  mensagem: string;
}

export interface SaidaResponse {
  clienteId: number;
  nome: string;
  saldoFinal: number;
  dataSaida: string;
  mensagem: string;
}
