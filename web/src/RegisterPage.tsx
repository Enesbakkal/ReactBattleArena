import AuthLayout from './AuthLayout'
import s from './AuthForm.module.scss'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from './api'



function RegisterPage() {
  const navigate = useNavigate()
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  // const [success, setSuccess] = useState('') success state'ini sil. Mesaj artık login'de.

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()// Gönder düğmesi formu gönderir. e.preventDefault() tarayıcının kendi GET isteğini keser. Bu satır olmazsa adres query string ile yenilenir ve apiFetch yarıda kalır.
    setError('')
    // setSuccess('') success state'ini sil. Mesaj artık login'de.

    try {
      // const response = await fetch('https://localhost:7275/api/auth/register', {
      //   method: 'POST',
      //   headers: {
      //     'Content-Type': 'application/json',
      //   },
      //   body: JSON.stringify({
      //     userName,
      //     email,
      //     displayName: displayName || null,
      //     password,
      //   }),
      // })             Burası kalktı çünkü ortak auth yazdık

      const response = await apiFetch('/api/auth/register', {
        method: 'POST',
        auth: false,
        body: {
          userName,
          email,
          displayName: displayName || null,
          password,
        }
      })  

      if (!response.ok) {
        setError('Kayıt başarısız (kullanıcı/email dolu veya validation)')
        return
      }
      navigate('/login', { state: { registered: true } })
    } catch { // Ağ yoksa, sertifika veya CORS yüzünden fetch reddederse catch çalışır. O dalda HTTP cevabı yoktur.
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
  }
//Input, metni kendi içinde tutmaz. value state'i gösterir, onChange state'i yazar. Buna controlled input denir.
return (
  <AuthLayout
    title="Battle Arena"
    subtitle="Yeni hesap aç."
  >
    <form className={s.form} onSubmit={handleSubmit}>
      <div>
        <label className={s.label} htmlFor="userName">
          Kullanıcı adı
        </label>
        <div className={s.field}>
          <input
            id="userName"
            className={s.input}
            type="text"
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
          />
        </div>
      </div>
      <div>
        <label className={s.label} htmlFor="email">
          E-posta
        </label>
        <div className={s.field}>
          <input
            id="email"
            className={s.input}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
      </div>
      <div>
        <label className={s.label} htmlFor="displayName">
          Görünen ad (opsiyonel)
        </label>
        <div className={s.field}>
          <input
            id="displayName"
            className={s.input}
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
          />
        </div>
      </div>
      <div>
        <label className={s.label} htmlFor="password">
          Şifre
        </label>
        <div className={s.field}>
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
        Kayıt ol
      </button>
      {error && <p className={s.error}>{error}</p>}
      <p className={s.hint}>Zaten hesabın var mı?</p>
      <Link className={s.link} to="/login">
        Giriş yap
      </Link>
    </form>
  </AuthLayout>
)
}

export default RegisterPage