import { api } from './api.js'

export const adminService = {
  async getUsers(search = '') {
    return (await api.get('/admin/users', { params: search ? { search } : undefined })).data
  },
  async assignRole(userId, role) {
    return (await api.put(`/admin/users/${userId}/role`, { role })).data
  },
}
