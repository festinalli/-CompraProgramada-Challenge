import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface LoginResponse {
  token: string;
  clienteId: number;
  nome: string;
  email: string;
  valorMensalAporte?: number;
  mensagem: string;
}

export interface SessionUser {
  clienteId: number;
  nome: string;
  email: string;
  valorMensalAporte: number;
  role: 'Cliente' | 'Admin';
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Relativo: funciona em dev (proxy) e no docker (nginx).
  private apiUrl = '/api/auth';
  private currentUserSubject = new BehaviorSubject<SessionUser | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) { }

  login(cpf: string, senha: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { CPF: cpf, Senha: senha })
      .pipe(tap(r => this.persist(r, 'Cliente')));
  }

  loginAdmin(usuario: string, senha: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login-admin`, { usuario, senha })
      .pipe(tap(r => this.persist(r, 'Admin')));
  }

  private persist(r: LoginResponse, role: 'Cliente' | 'Admin'): void {
    if (!r?.token) { return; }
    const user: SessionUser = {
      clienteId: r.clienteId, nome: r.nome, email: r.email,
      valorMensalAporte: r.valorMensalAporte ?? 0, role
    };
    localStorage.setItem('token', r.token);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): SessionUser | null {
    return this.currentUserSubject.value;
  }

  isAdmin(): boolean {
    return this.currentUserSubject.value?.role === 'Admin';
  }

  private getUserFromStorage(): SessionUser | null {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) as SessionUser : null;
  }
}
