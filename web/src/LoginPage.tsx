import AuthLayout from './AuthLayout'
import { useState } from 'react'
import { Link, useNavigate, useLocation} from 'react-router-dom' // useLocationu register olununca başarılı sonucunu gösterebilmek için ekledik
import { apiFetch, setToken, setRefreshToken } from './api'
import s from './AuthForm.module.scss'


function LoginPage() {
  const location = useLocation()
  const registered =
    (location.state as { registered?: boolean } | null)?.registered === true
  const navigate = useNavigate()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    //e.preventDefault() tarayıcının kendi form GET'ini keser. Ardından hata metni temizlenir ve apiFetch çağrılır. auth: false bu isteğe Bearer koymaz. Gövde userNameOrEmail ve password alanlarıdır.
    setError('')

    try {
      // const response = await fetch('https://localhost:7275/api/auth/login', {
      //   method: 'POST',
      //   headers: {
      //     'Content-Type': 'application/json',
      //   },
      //   body: JSON.stringify({
      //     userNameOrEmail,
      //     password,
      //   }),
      // })

      // if (!response.ok) {
      //   setError('Giriş başarısız')
      //   return
      // }

      // const data = await response.json()
      // localStorage.setItem('token', data.token)
      
      // navigate('/characters')
      // //onLogin()
      // //Ne için: onLogin = “token kaydedildi, artık karakter sayfasını göster” sinyali. Props = dışarıdan gelen parametre (Faz 0).  BU kısma gerek kalmadı çünkü ortak auth yazdık

      const response = await apiFetch('/api/auth/login',{
        method: 'POST',
        auth: false,
        body: {
          userNameOrEmail,
          password
        },
      })  //HttpClient karşılığı, JSON body yazıp Authorization header'ı eklememektir. Üye ol ile aynı apiFetch kullanılır. Fark, adres ve gövdedir.

      //400 ve 401 ikisi de response.ok değildir. Sayfa gövdeyi ayırmaz. 
      // Müşteri Giriş başarısız görür. Scalar'da 400 alan hatalarını, 401 boş gövdeyi ayrı gösterir. 
      // Ağ, sertifika veya CORS yüzünden fetch reddederse catch çalışır.

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters') // /characters rotası AppLayout App.tsx üzerinden altındadır.

    } catch (err) {
      console.error(err)
      setError('API’ye ulaşılamadı (backend çalışıyor mu? F12 Console’a bak)')
    }
  }

  //Kullanıcı adı kutusu controlled input'tur. Metin state'te durur. Parola kutusunda type="password" vardır. Ekranda gizlenir, state'te ve JSON'da düz metin durur.
  return (
  <AuthLayout
    title="Battle Arena"
    subtitle="Kullanıcı adı veya e-posta ile giriş yap."
  >
    <form className={s.form} onSubmit={handleSubmit}>
      <div>
        {registered && (
          <p className={s.success}>Yeni kullanıcın oluşturuldu. Giriş yapabilirsin.</p>
        )} 
        {/* && burada da error && ile aynı işi görür. Koşul true değilse React paragrafı çizmez. */}
        <label className={s.label} htmlFor="userNameOrEmail">
          Kullanıcı adı veya e-posta
        </label>
        <div className={s.field}>
          <svg className={s.icon} viewBox="0 0 24 24" aria-hidden="true">
            <circle cx="12" cy="8" r="3.2" fill="none" stroke="currentColor" strokeWidth="1.6" />
            <path d="M5 19c1.4-3 3.8-4.5 7-4.5s5.6 1.5 7 4.5" fill="none" stroke="currentColor" strokeWidth="1.6" />
          </svg>
          <input
            id="userNameOrEmail"
            className={s.input}
            type="text"
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
          />
        </div>
      </div>
      <div>
        <label className={s.label} htmlFor="password">
          Şifre
        </label>
        <div className={s.field}>
          <svg className={s.icon} viewBox="0 0 24 24" aria-hidden="true">
            <rect x="5" y="11" width="14" height="9" rx="2" fill="none" stroke="currentColor" strokeWidth="1.6" />
            <path d="M8 11V8a4 4 0 0 1 8 0v3" fill="none" stroke="currentColor" strokeWidth="1.6" />
          </svg>
          <input
            id="password"
            className={s.input}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
      </div>
      <button className={s.submit} type="submit">
        Giriş
      </button>
      {error && <p className={s.error}>{error}</p>}
      <p className={s.hint}>Hesabın yok mu?</p>
      <Link className={s.link} to="/register">
        Kayıt ol
      </Link>
    </form>
  </AuthLayout>
)

}

export default LoginPage