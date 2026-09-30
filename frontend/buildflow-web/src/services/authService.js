import { api } from './api.js'

export const authService = {
  async register(payload) { return (await api.post('/auth/register', payload)).data },
  async login(payload) { return (await api.post('/auth/login', payload)).data },
  async me() { return (await api.get('/auth/me')).data },
  async logout() { await api.post('/auth/logout') },
}
