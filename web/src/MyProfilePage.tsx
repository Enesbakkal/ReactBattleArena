import UserProfilePanel from './UserProfilePanel'
import { useUserId } from './PermissionContext'

function MyProfilePage() {
  const userId = useUserId()

  if (!userId) return <p>Oturum bilgisi yok</p>

  return <UserProfilePanel userId={userId} />
}

export default MyProfilePage