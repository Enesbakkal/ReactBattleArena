import { useState } from 'react'

interface LoginPageProps {
  onLogin: () => void
}

function LoginPage({ onLogin }: LoginPageProps) {
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    try {
      const response = await fetch('https://localhost:7275/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userNameOrEmail,
          password,
        }),
      })

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      localStorage.setItem('token', data.token)
      onLogin()
      //Ne için: onLogin = “token kaydedildi, artık karakter sayfasını göster” sinyali. Props = dışarıdan gelen parametre (Faz 0).

    } catch {
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Kullanıcı adı veya e-posta
          <input
            type="text"
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
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

      <button type="submit">Giriş</button>
      {error && <p>{error}</p>}
    </form>
  )
}

export default LoginPage