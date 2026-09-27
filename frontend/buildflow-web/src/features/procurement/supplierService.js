import { api } from '../../services/api.js'

export const supplierService = {
  suppliers: async () => (await api.get('/Suppliers')).data,

  supplier: async (id) => (await api.get(`/Suppliers/${id}`)).data,

  createSupplier: async (body) =>
    (await api.post('/Suppliers', body)).data,

  updateSupplier: async (id, body) =>
    (await api.put(`/Suppliers/${id}`, body)).data,

  deleteSupplier: async (id) =>
    (await api.delete(`/Suppliers/${id}`)).data,
}
