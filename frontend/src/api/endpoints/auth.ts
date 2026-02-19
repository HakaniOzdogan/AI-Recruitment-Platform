import { http } from "../http";

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  fullName: string;
  email: string;
  password: string;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
};

export async function loginRequest(payload: LoginRequest): Promise<LoginResponse> {
  const response = await http.post<LoginResponse>("/auth/login", payload);
  return response.data;
}

export async function registerRequest(payload: RegisterRequest): Promise<LoginResponse> {
  const response = await http.post<LoginResponse>("/auth/register", payload);
  return response.data;
}
