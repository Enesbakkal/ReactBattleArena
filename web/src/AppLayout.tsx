import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom'
import './AppLayout.css'

function AppLayout() {
  const navigate = useNavigate()
  const token = localStorage.getItem('token')

  if (!token) {
    return <Navigate to="/login" replace />
  }

  function handleLogout() {
    localStorage.removeItem('token')
    navigate('/login')
  }

  return (
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
            <button type="button" onClick={handleLogout}>
                Çıkış
            </button>
        </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}

export default AppLayout