const SESSION_KEY = 'buildflow.session'

function storageFor(remember) {
  return remember ? window.localStorage : window.sessionStorage
}

export function getSession() {
  const serialized = window.localStorage.getItem(SESSION_KEY) ?? window.sessionStorage.getItem(SESSION_KEY)
  if (!serialized) return null
  try {
    return JSON.parse(serialized)
  } catch {
    clearSession()
    return null
  }
}

export function saveSession(session, remember = getSession()?.remember ?? false) {
  clearSession()
  storageFor(remember).setItem(SESSION_KEY, JSON.stringify({ ...session, remember }))
}

export function clearSession() {
  window.localStorage.removeItem(SESSION_KEY)
  window.sessionStorage.removeItem(SESSION_KEY)
}

export function getAccessToken() {
  return getSession()?.accessToken ?? null
}

export function getRefreshToken() {
  return getSession()?.refreshToken ?? null
}
