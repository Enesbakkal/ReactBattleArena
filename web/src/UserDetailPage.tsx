import { useParams } from 'react-router-dom'
import UserProfilePanel from './UserProfilePanel'

function UserDetailPage() {
  const { id } = useParams()

  if (!id) return <p>Kullanıcı bulunamadı</p>

  return <UserProfilePanel userId={id} />
}

export default UserDetailPage