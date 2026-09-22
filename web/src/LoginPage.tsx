import { useState } from 'react'
import { Link, useNavigate} from 'react-router-dom'
import { apiFetch, setToken, setRefreshToken } from './api'


function LoginPage() {
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
      })

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')

    } catch (err) {
      console.error(err)
      setError('API’ye ulaşılamadı (backend çalışıyor mu? F12 Console’a bak)')
    }
  }

  //Kullanıcı adı kutusu controlled input'tur. Metin state'te durur. Parola kutusunda type="password" vardır. Ekranda gizlenir, state'te ve JSON'da düz metin durur.
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
      <p>
        <Link to="/register">Kayıt ol</Link>
      </p>
    </form>
  )

}

export default LoginPage