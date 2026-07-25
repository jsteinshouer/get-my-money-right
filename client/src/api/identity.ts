import { apiClient } from './client'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
}

export const identityApi = {
  login: (email: string, password: string) =>
    apiClient.post<CurrentUser>('/identity/login', { email, password }),
  logout: () => apiClient.post<void>('/identity/logout'),
  me: () => apiClient.get<CurrentUser>('/identity/me'),
}
