import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from './LoginPage'
import RegisterPage from './RegisterPage'
import CharactersPage from './CharactersPage'
import CharacterCreatePage from './CharacterCreatePage'
import CharacterDetailPage from './CharacterDetailPage'
import CharacterEditPage from './CharacterEditPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/characters" element={<CharactersPage />} />
      <Route path="/characters/new" element={<CharacterCreatePage />} />
      <Route path="/characters/:id" element={<CharacterDetailPage />} />
      <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
      <Route path="/" element={<Navigate to="/characters" replace />} />
      <Route path="*" element={<Navigate to="/characters" replace />} />
    </Routes>
  )
}

export default App