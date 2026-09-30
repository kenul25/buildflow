import { api } from './api.js'

export const adminService = {
  async getUsers(search = '') {
    return (await api.get('/admin/users', { params: search ? { search } : undefined })).data
  },
  async createUser(payload) {
    return (await api.post('/admin/users', payload)).data
  },
  async updateUser(userId, payload) {
    return (await api.put(`/admin/users/${userId}`, payload)).data
  },
  async assignRole(userId, role) {
    return (await api.put(`/admin/users/${userId}/role`, { role })).data
  },
  async deleteUser(userId) {
    await api.delete(`/admin/users/${userId}`)
  },
}
