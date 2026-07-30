import { useState } from 'react'
import LoginPage from './LoginPage'
import RegisterPage from './RegisterPage'
import CharactersPage from './CharactersPage'

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(
    () => !!localStorage.getItem('token'),
  )
  const [authView, setAuthView] = useState<'login' | 'register'>('login')

  if (!isLoggedIn) {
    if (authView === 'register') {
      return (
        <RegisterPage
          onRegistered={() => setAuthView('login')}
          onBack={() => setAuthView('login')}
        />
      )
    }

    return (
      <LoginPage
        onLogin={() => setIsLoggedIn(true)}
        onGoRegister={() => setAuthView('register')}
      />
    )
  }

  return <CharactersPage />
}

export default App