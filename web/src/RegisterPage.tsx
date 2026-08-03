import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'



function RegisterPage() {
  const navigate = useNavigate()
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')

    try {
      const response = await fetch('https://localhost:7275/api/auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userName,
          email,
          displayName: displayName || null,
          password,
        }),
      })

      if (!response.ok) {
        setError('Kayıt başarısız (kullanıcı/email dolu veya validation)')
        return
      }

      setSuccess('Kayıt OK — şimdi giriş yap')
      navigate('/login')
    } catch {
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
  }

return (
    <div>
      <h1>Kayıt</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label>
            Kullanıcı adı
            <input
              type="text"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
            />
          </label>
        </div>
        <div>
          <label>
            E-posta
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
        </div>
        <div>
          <label>
            Görünen ad (opsiyonel)
            <input
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
            />
          </label>
        </div>
        <div>
          <label>
            Şifre
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
        </div>
        <button type="submit">Kayıt ol</button>
      </form>
      {error && <p>{error}</p>}
      {success && <p>{success}</p>}
      <p>
        <Link to="/login">Girişe dön</Link>
      </p>
    </div>
  )
}

export default RegisterPage