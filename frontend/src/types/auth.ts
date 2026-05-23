export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface TokenResponse {
  accessToken: string;
  expiresAt: string;
}

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  roles: string[];
}
