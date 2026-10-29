import { useEffect, useState } from 'react'
import { Navigate, Outlet, useNavigate } from 'react-router-dom'
// import './AppLayout.css'  burayı kaldırdık çünkü artık complex template'E geçiyoruz
import { apiFetch, clearToken, getToken, logout } from './api'
import { PermissionContext } from './PermissionContext'
import Header from './shell/Header'
import Sidebar from './shell/Sidebar'
import shell from './shell/Shell.module.scss'


//Sayfanın çalışma mantığı çok önemli 
// Bu iki if aynı anda çalışmaz. AppLayout fonksiyonu yukarıdan aşağı iner. 
// useEffect satırına gelince loadMe çağrılmaz. React, fonksiyonu saklar. Saklanan fonksiyon, return ile ekran çizildikten sonra çalışır.Çizim sırasında sıra şöyledir. 
// getToken() localStorage anahtarı token değerini okur. Anahtar yoksa 44. satırdaki if girer ve Navigate ile /login adresine döner. /me bu çizimde atılmaz. 
// Anahtar varsa 44. satır geçilir. meLoaded hâlâ false olduğu için ekrana Yükleniyor… yazılır.Çizim bitince saklanan useEffect fonksiyonu çalışır. 
// Onun ilk satırı da if (!token) return olur. Anahtar yoksa loadMe hiç çağrılmaz. Anahtar duruyorsa loadMe çalışır ve GET /api/auth/me atılır. 
// apiFetch varsayılanı auth: true olduğu için header Authorization: Bearer ve ardından access token olur. Arada bir boşluk vardır. Bearer kelimesi şemanın adıdır.

function AppLayout() {
  const navigate = useNavigate()
  // const token = localStorage.getItem('token') ortak auth
  const token = getToken() // Layout önce getToken() okur. Access token yoksa bileşen Navigate ile /login adresine döner. Liste çizilmez.
  const [permissions, setPermissions] = useState<string[]>([])
  const [userId, setUserId] = useState<string | null>(null)
  const [meLoaded, setMeLoaded] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(true)

  useEffect(() => {
  if (!token) {
    return
  }

  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
        setUserId(me.id ?? null)
      } else if (meResponse.status === 401) {
        // apiFetch buraya gelene kadar yenilemeyi denedi ve başaramadı: oturum bitti.
        clearToken()
        navigate('/login')
        return
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }

  loadMe()
}, [token])
  

  if (!token) {
    return <Navigate to="/login" replace />
    //Bu kontrol sunucuya gitmez. Tarayıcıdaki token anahtarına bakar. 
    // Anahtar duruyorsa layout GET /api/auth/me atar. apiFetch varsayılanı auth: true olduğu için header Authorization: Bearer ve ardından access token olur. Arada bir boşluk vardır. 
    // Bearer kelimesi şemanın adıdır.
  }

  if (!meLoaded) {
  return <p>Yükleniyor…</p>
}

  // function handleLogout() {
  //   localStorage.removeItem('token')
  //   navigate('/login')
  // }  Ortak auth

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
  <PermissionContext.Provider value={{ permissions, userId }}>
    <div className={shell.root}>
      <Sidebar open={sidebarOpen} />
      <div className={sidebarOpen ? shell.wrap : `${shell.wrap} ${shell.wrapClosed}`}>
        <Header
          onToggleSidebar={() => setSidebarOpen((open) => !open)}
          onLogout={handleLogout}
        />
        <main className={shell.content}>
          <Outlet />
        </main>
      </div>
    </div>
  </PermissionContext.Provider>
)
}

export default AppLayout
