import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './AppLayout'
import LoginPage from './LoginPage'
import RegisterPage from './RegisterPage'
import CharactersPage from './CharactersPage'
import CharacterCreatePage from './CharacterCreatePage'
import CharacterDetailPage from './CharacterDetailPage'
import CharacterEditPage from './CharacterEditPage'
import UsersPage from './UsersPage'
import MyProfilePage from './MyProfilePage'
import EditProfilePage from './EditProfilePage'
import ChangePasswordPage from './ChangePasswordPage'
import UserDetailPage from './UserDetailPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/users/:id" element={<UserDetailPage />} />
        <Route path="/profile" element={<MyProfilePage />} />
        <Route path="/profile/edit" element={<EditProfilePage />} />
        <Route path="/profile/password" element={<ChangePasswordPage />} />
      </Route>

      <Route path="/" element={<Navigate to="/characters" replace />} />
      <Route path="*" element={<Navigate to="/characters" replace />} />
    </Routes>
  )
}
//Rota App.tsx içindedir. /characters/:id/edit, /characters/:id rotasından önce durur. Daha genel rota önce gelseydi edit kelimesi bir id sanılırdı.

export default App