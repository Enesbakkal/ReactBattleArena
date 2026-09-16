export const API_BASE = 'https://localhost:7275'

export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}

export function clearToken() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}

export function getRefreshToken(): string | null {
  return localStorage.getItem('refreshToken')
}
export function setRefreshToken(token: string) {
  localStorage.setItem('refreshToken', token)
}

let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }

  refreshInFlight = (async () => {
    const refreshToken = getRefreshToken()
    if (!refreshToken) {
      return false
    }

    const response = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      clearToken()
      return false
    }

    const data = await response.json()
    setToken(data.token)
    setRefreshToken(data.refreshToken)
    return true
  })()

  try {
    return await refreshInFlight
  } finally {
    refreshInFlight = null
  }
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()

  if (refreshToken) {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // API kapalıysa bile yerel temizlik yapılmalı; kullanıcı ekranda kalmasın
      //Üç ayrıntı var burada. Token yoksa isteği hiç atmıyoruz, çünkü validator boş değere 400 döner ve çıkış yaparken hata görmek anlamsız. 
      // İstek apiFetch değil düz fetch; apiFetch kullanırsak 401 ihtimalinde refresh denemesi yapar, oysa biz tam tersini istiyoruz. 
      // clearToken() de try/catch'in dışında, yani sunucuya ulaşılamasa bile tarayıcı temizlenir.
    }
  }

  clearToken()
}

type ApiFetchOptions = {
  method?: string
  body?: unknown
  /** false = login/register (Bearer yok). Varsayılan true. */
  auth?: boolean
}

export async function apiFetch(
  path: string,
  options: ApiFetchOptions = {},
): Promise<Response> {
  const { method = 'GET', body, auth = true } = options

  const headers: Record<string, string> = {}// c# daki record değil dictionary

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {// Login yaparken auth olmadığı için buraya girmez
    // Backend'e henüz gidilmedi. JWT bu satırda Api'den gelmez.
    // Login'de gelmişti: POST /api/auth/login → data.token → setToken → localStorage 'token'.
    // Şimdi çekmeceden okuyoruz, header'a yazıyoruz, ONDAN SONRA alttaki fetch gider.
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  // İstek burada backende çıkar (7275). Header'da Bearer varsa Api JWT'yi burada görür.
  // Cevap bu return: status + gövde. JSON'u sayfa response.json() ile açar.
  // Login/register auth: false olduğu için yukarıdaki if çalışmaz; o istek tokensız gider, JWT cevapta gelir.
  
  //  return fetch(`${API_BASE}${path}`, {  // Burayı yorum yaptık çünkü refresh tokenın sessizce yenilenme mantığını eklemek istedik
  //    method,
  //   headers,
  //    body: body !== undefined ? JSON.stringify(body) : undefined,
  //  })

  const response = await fetch(`${API_BASE}${path}`, {
  method,
  headers,
  body: body !== undefined ? JSON.stringify(body) : undefined,
  })
  if (response.status !== 401 || auth === false) {
    return response
  }
  const refreshed = await refreshSession()
  if (!refreshed) {
    return response
  }
  const retryHeaders: Record<string, string> = { ...headers }
  const newToken = getToken()
  if (newToken) {
    retryHeaders.Authorization = `Bearer ${newToken}`
  }
  return fetch(`${API_BASE}${path}`, {
    method,
    headers: retryHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

//   Burada üç ayrıntı önemli. refreshSession içindeki istek bilinçli olarak apiFetch değil düz fetch; apiFetch kullanırsan 401 gelirse o da refreshSession çağırır ve kendi kuyruğunda kilitlenir. 
// refreshInFlight değişkeni, iki istek aynı anda 401 alırsa ikisinin de aynı fişi harcamasını engelliyor; ikinci istek yeni bir yenileme başlatmaz, birincinin sonucunu bekler. 
// Son olarak koşulda sadece 401 var, 403 yok: 403 "yetkin yok" demektir ve token yenilemek onu düzeltmez.
// Tekrar isteğinde retryHeaders ile yeni token'ı koyuyoruz; eski headers nesnesini olduğu gibi göndermek en sık yapılan hata, çünkü içinde ölü JWT durur ve ikinci kez 401 alırsın.
  
}