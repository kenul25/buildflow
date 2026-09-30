import axios from 'axios'
import { clearSession, getAccessToken, getRefreshToken, getSession, saveSession } from './tokenService.js'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5144/api'
export const api = axios.create({ baseURL, timeout: 15000 })
let refreshPromise = null

api.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const request = error.config
    const refreshToken = getRefreshToken()
    const skipsRefresh = ['/auth/login', '/auth/register', '/auth/refresh'].includes(request?.url)
    if (error.response?.status !== 401 || request?._retried || skipsRefresh || !refreshToken) {
      return Promise.reject(error)
    }

    request._retried = true
    refreshPromise ??= axios
      .post(`${baseURL}/auth/refresh`, { refreshToken })
      .then(({ data }) => {
        saveSession(data, getSession()?.remember)
        return data.accessToken
      })
      .catch((refreshError) => {
        clearSession()
        window.dispatchEvent(new Event('buildflow:session-expired'))
        throw refreshError
      })
      .finally(() => { refreshPromise = null })

    request.headers.Authorization = `Bearer ${await refreshPromise}`
    return api(request)
  },
)

export function apiErrorMessage(error) {
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'
  return error.response.data?.detail ?? 'The request could not be completed.'
}
