import { api } from './api.js'

// Follow pages rather than silently truncating selectors at 100 records.
export async function allRows(path, params = {}) {
  const rows = []
  for (let page = 1; ; page++) {
    const response = await api.get(path, { params: { ...params, page, pageSize: 100 } })
    const data = response.data
    const items = Array.isArray(data) ? data : data.items ?? []
    rows.push(...items)
    const total = Array.isArray(data) ? Number(response.headers?.['x-total-count']) : data.total
    if (items.length < 100 || Number.isFinite(total) && rows.length >= total || Array.isArray(data) && !Number.isFinite(total)) return rows
  }
}
