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

let refreshInFlight: Promise<boolean> | null = null // refreshInFlight doluysa ikinci çağrı yeni bir POST /api/auth/refresh açmaz. Aynı Promise'i bekler.
// refreshInFlight refreshSession içindedir. 
// İki karakter isteği aynı anda 401 alırsa ikisi de refreshSession çağırır. 
// Birincisi Promise'i değişkene yazar ve POST /api/auth/refresh açar. 
// İkincisi değişken dolu olduğu için aynı Promise'i bekler. İkinci bir ham token göndermez.

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }

  refreshInFlight = (async () => {
    const refreshToken = getRefreshToken()
    if (!refreshToken) {
      return false //Ham token yoksa fonksiyon false döner. POST atılmaz. apiFetch ilk 401 cevabını sayfaya bırakır.
    }

    const response = await fetch(`${API_BASE}/api/auth/refresh`, { // İstek apiFetch değildir. Düz fetch olur. apiFetch olsaydı bu POST da 401 alınca yine refreshSession çağırırdı. refreshSession kendi isteğinin bitmesini beklerdi.
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }), // Gövde { refreshToken } olur. Header'da Bearer yoktur.
    })

    if (!response.ok) {
      clearToken() // Cevap 200 değilse clearToken hem token hem refreshToken anahtarını siler ve false döner.
      return false
    }
    // 200 ise JSON'daki token yeni access token'dır, refreshToken yeni ham refresh token'dır. 
    //finally bloğu refreshInFlight değişkenini null yapar. Bekleyen ikinci çağrı da bu true veya false sonucunu alır.

    const data = await response.json()
    setToken(data.token)
    setRefreshToken(data.refreshToken)
    //refreshSession setToken ve setRefreshToken ile iki anahtarı yazar. apiFetch asıl isteği yeni Bearer ile tekrar atar.
    return true
  })()

  try {
    return await refreshInFlight
  } finally {
    refreshInFlight = null
  }
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()// logout getRefreshToken okur. Ham string yoksa POST atılmaz. clearToken yine çalışır.

  if (refreshToken) {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, { //Ham string varsa istek düz fetch olur. apiFetch kullanılmaz. Gövde { refreshToken } olur. Header'da Bearer yoktur.
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
  //Ağ hatası catch içinde yutulur. clearToken try bloğunun dışındadır. API kapalı olsa da iki anahtar silinir.
  //Controller handler'ın bool sonucunu okumaz. false da true da 204 olur. clearToken tarayıcıdaki access token string'ini ve ham refresh token'ı siler. 
  // Access token tabloda bir satır değildir. API o JWT'yi iptal etmez. Süre dolana kadar aynı string [Authorize] metotlarda geçer.
  //fetch cevabının status'una bakılmaz. 204 de 400 de 500 de clearToken'a düşer. catch yalnız ağın hiç cevap vermediği durumu yakalar. O durumda da clearToken çalışır.

}

type ApiFetchOptions = {
  method?: string
  body?: unknown
  /** false = login/register (Bearer yok). Varsayılan true. */
  auth?: boolean
}

export async function apiFetch(// tarayıcının HttpClient'ı olarak düşünebiliriz
  path: string,
  options: ApiFetchOptions = {},
): Promise<Response> {
  const { method = 'GET', body, auth = true } = options // auth gönderilmediği zaman optionstan atanmaz ve default değer olan true olur

  const headers: Record<string, string> = {}// c# daki record değil dictionary

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {// Login yaparken auth(false) olmadığı için buraya girmez
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
  if (response.status !== 401 || auth === false) { // loginde auth false olduğu için refresh kısmına hiç girmez
    return response
  }

  //register:  apiFetch, cevap 401 ise ve auth true ise POST /api/auth/refresh dener. Register'da auth false olduğu için bu dal çalışmaz. Kayıt cevabı 401 beklemez. Access token bitince yenileme, oturumu uzatma işidir.
  const refreshed = await refreshSession()
  //refreshSession true dönerse retryHeaders eski header kopyasıdır. 
  // İçindeki Bearer hâlâ süresi dolmuş access token'dır. 
  // getToken yenilemeden sonra localStorage'a yazılmış yeni access token'ı okur. retryHeaders.Authorization o yeni string ile değiştirilir. 
  // İkinci fetch aynı path, aynı metot ve aynı gövde ile gider.
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