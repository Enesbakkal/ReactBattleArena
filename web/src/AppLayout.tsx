import { useEffect, useState } from 'react'
import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom'
import './AppLayout.css'
import { apiFetch, clearToken, getToken } from './api'
import { PermissionContext } from './PermissionContext'




function AppLayout() {
  const navigate = useNavigate()
  // const token = localStorage.getItem('token') ortak auth
  const token = getToken()
  const [permissions, setPermissions] = useState<string[]>([])
  const [meLoaded, setMeLoaded] = useState(false)

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
  }

  if (!meLoaded) {
  return <p>Yükleniyor…</p>
}

  // function handleLogout() {
  //   localStorage.removeItem('token')
  //   navigate('/login')
  // }  Ortak auth

  function handleLogout() {
    clearToken()
    navigate('/login')
  }

  return (
    <PermissionContext.Provider value={{ permissions }}>
      <div className="app-shell">
        <header className="app-header">
              <div className="app-header__left">
                  <Link to="/characters" className="app-header__brand">
                  ReactBattleArena
                  </Link>
                  <nav className="app-header__nav">
                  <Link to="/characters">Karakterler</Link>
                  </nav>
              </div>
              <button
                type="button"
                className="app-header__logout"
                onClick={handleLogout}
              >
                Çıkış
              </button>
          </header>
        <main className="app-main">
          <Outlet />
        </main>
    </div>
    </PermissionContext.Provider>

    
  )
}

export default AppLayout