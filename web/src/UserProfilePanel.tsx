import { useEffect, useState } from 'react'
import { apiFetch } from './api'
import s from './UserProfilePanel.module.scss'

type UserDetail = {
  id: string
  userName: string
  email: string | null
  displayName: string | null
  firstName: string | null
  lastName: string | null
  phone: string | null
  avatarUrl: string | null
  isDisabled: boolean
  isContactPublic: boolean
  roles: string[]
  points: number
}

type Props = {
  userId: string
}

function UserProfilePanel({ userId }: Props) {
  const [user, setUser] = useState<UserDetail | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const response = await apiFetch(`/api/users/${userId}`)
        if (!response.ok) {
          if (!cancelled) {
            setError(response.status === 404 ? 'Kullanıcı bulunamadı' : 'Profil alınamadı')
            setUser(null)
          }
          return
        }
        const data = await response.json()
        if (!cancelled) setUser(data)
      } catch {
        if (!cancelled) setError('API’ye ulaşılamadı')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [userId])

  if (loading) return <p>Yükleniyor…</p>
  if (error) return <p className={s.error}>{error}</p>
  if (!user) return null

  const fullName =
    [user.firstName, user.lastName].filter(Boolean).join(' ') ||
    user.displayName ||
    user.userName

  return (
    <div className={s.layout}>
      <section className={s.card}>
        <div className={s.cover} />
        <div className={s.body}>
          {user.avatarUrl ? (
            <img className={s.avatar} src={user.avatarUrl} alt="" />
          ) : (
            <span className={s.avatarEmpty} />
          )}
          <h1 className={s.name}>{fullName}</h1>
          <p className={s.userName}>@{user.userName}</p>
          <ul className={s.meta}>
            <li>E-posta: {user.email ?? '—'}</li>
            <li>Telefon: {user.phone ?? '—'}</li>
            <li>Rol: {user.roles.length > 0 ? user.roles.join(', ') : '—'}</li>
            <li>Puan: {user.points}</li>
            <li>Disabled: {user.isDisabled ? 'Yes' : 'No'}</li>
          </ul>
        </div>
      </section>

      <section className={s.activities}>
        <h2>Activities</h2>
        <p className={s.hint}>
          Yazı ve yorum bu dilimde yok. Kolon yer tutucu.
        </p>
      </section>
    </div>
  )
}

export default UserProfilePanel