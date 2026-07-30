import { useState } from 'react'
import LoginPage from './LoginPage'
import CharactersPage from './CharactersPage'

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(
    () => !!localStorage.getItem('token'),
  )

  if (!isLoggedIn) {
    return <LoginPage onLogin={() => setIsLoggedIn(true)} />
  }

  return <CharactersPage />
}

export default App