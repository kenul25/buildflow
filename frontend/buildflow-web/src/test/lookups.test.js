import { beforeEach, expect, test, vi } from 'vitest'
import { api } from '../services/api.js'
import { allRows } from '../services/lookups.js'
vi.mock('../services/api.js', () => ({ api: { get: vi.fn() } }))
beforeEach(() => vi.clearAllMocks())
test('follows paged array responses with an exposed total count', async () => {
  api.get.mockResolvedValueOnce({ data: Array.from({ length: 100 }, (_, id) => ({ id })), headers: { 'x-total-count': '101' } }).mockResolvedValueOnce({ data: [{ id: 100 }], headers: { 'x-total-count': '101' } })
  expect(await allRows('/PurchaseRequests')).toHaveLength(101)
  expect(api.get).toHaveBeenCalledTimes(2)
})
test('unpaged legacy arrays terminate even with 100 rows', async () => {
  api.get.mockResolvedValue({ data: Array.from({ length: 100 }, (_, id) => ({ id })) })
  expect(await allRows('/legacy')).toHaveLength(100)
  expect(api.get).toHaveBeenCalledTimes(1)
})
