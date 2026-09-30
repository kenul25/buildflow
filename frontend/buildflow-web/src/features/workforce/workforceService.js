import { api } from '../../services/api.js'

export const workforceService = {
  // Workers
  listWorkers: async (params) =>
    (await api.get('/workers', { params })).data,

  getWorker: async (id) =>
    (await api.get(`/workers/${id}`)).data,

  createWorker: async (body) =>
    (await api.post('/workers', body)).data,

  updateWorker: async (id, body) =>
    (await api.put(`/workers/${id}`, body)).data,

  archiveWorker: async (id) =>
    api.delete(`/workers/${id}`),
}