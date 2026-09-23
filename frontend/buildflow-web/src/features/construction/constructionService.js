import { api } from '../../services/api.js'

export const kinds = {
  projects: { title: 'Projects', parent: null },
  sites: { title: 'Sites', parent: 'projects' },
  phases: { title: 'Construction phases', parent: 'sites' },
  activities: { title: 'Construction activities', parent: 'phases' },
}

export const constructionService = {
  list: async (kind, params) => (await api.get(`/${kind}`, { params })).data,
  get: async (kind, id) => (await api.get(`/${kind}/${id}`)).data,
  create: async (kind, body) => (await api.post(`/${kind}`, body)).data,
  update: async (kind, id, body) => (await api.put(`/${kind}/${id}`, body)).data,
  archive: async (kind, id) => api.delete(`/${kind}/${id}`),
  engineers: async () => (await api.get('/construction/engineers')).data,
}
