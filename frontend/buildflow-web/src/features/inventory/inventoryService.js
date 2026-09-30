import { api } from '../../services/api.js'

export const inventoryService = {
  materials: async (params = {}) => (await api.get('/inventory/materials/page', { params })).data,
  material: async (id) => (await api.get(`/inventory/materials/${id}`)).data,
  createMaterial: async (body) => (await api.post('/inventory/materials', body)).data,
  updateMaterial: async (id, body) => (await api.put(`/inventory/materials/${id}`, body)).data,
  deleteMaterial: async (id) => api.delete(`/inventory/materials/${id}`),
  warehouses: async (params = {}) => (await api.get('/inventory/warehouses/page', { params })).data,
  createWarehouse: async (body) => (await api.post('/inventory/warehouses', body)).data,
  updateWarehouse: async (id, body) => (await api.put(`/inventory/warehouses/${id}`, body)).data,
  deleteWarehouse: async (id) => api.delete(`/inventory/warehouses/${id}`),
  stock: async (id, action, body) => (await api.post(`/inventory/materials/${id}/${action}`, body)).data,
  alerts: async (threshold = 0) => (await api.get('/inventory/alerts/low-stock', { params: { threshold } })).data,
  reservations: async (params = {}) => (await api.get('/inventory/reservations', { params })).data,
  reserve: async (id, body) => (await api.post(`/inventory/materials/${id}/reservations`, body)).data,
  release: async (id) => api.delete(`/inventory/reservations/${id}`),
  movements: async (params = {}) => (await api.get('/inventory/movements', { params })).data,
}