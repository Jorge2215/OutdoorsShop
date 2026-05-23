import apiClient from './client';
import type { LoginRequest, RegisterRequest, TokenResponse, AuthUser } from '../types/auth';

export const authApi = {
  login: (data: LoginRequest) => apiClient.post<TokenResponse>('/auth/login', data),
  register: (data: RegisterRequest) => apiClient.post<TokenResponse>('/auth/register', data),
  refresh: () => apiClient.post<TokenResponse>('/auth/refresh'),
  logout: () => apiClient.post('/auth/logout'),
  me: () => apiClient.get<AuthUser>('/auth/me'),
};
