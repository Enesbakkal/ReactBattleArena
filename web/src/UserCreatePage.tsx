import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { apiFetch } from './api'
import { hasPermission, PERMISSIONS } from './permissions'
import { usePermissions } from './PermissionContext'
import s from './EditProfilePage.module.scss'

function UserCreatePage() {
  const navigate = useNavigate()
  const permissions = usePermissions()

  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  if (!hasPermission(permissions, PERMISSIONS.usersCreate)) {
    return <Navigate to="/users" replace />
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSaving(true)

    try {
      const response = await apiFetch('/api/users', {
        method: 'POST',
        body: {
          userName,
          email,
          displayName: displayName.trim() || null,
          password,
        },
      })

      if (response.status === 403) {
        setError('Forbidden')
        return
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null)
        const messages = problem?.errors
          ? Object.values(problem.errors).flat().join(' | ')
          : problem?.title ?? `Error ${response.status}`
        setError(String(messages))
        return
      }

      navigate('/users')
    } catch {
      setError('Could not reach API')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={s.page}>
      <div className={s.headerRow}>
        <h1>Add User</h1>
        <Link className={s.back} to="/users">
          Back to list
        </Link>
      </div>
      {error && <p className={s.error}>{error}</p>}

      <form className={s.form} onSubmit={handleSubmit}>
        <label className={s.field}>
          User Name
          <input
            required
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
          />
        </label>
        <label className={s.field}>
          Email
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        <label className={s.field}>
          Display Name
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
          />
        </label>
        <label className={s.field}>
          Password
          <input
            type="password"
            required
            minLength={6}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <div className={s.actions}>
          <button className={s.save} type="submit" disabled={saving}>
            Save
          </button>
          <button
            className={s.secondary}
            type="button"
            disabled={saving}
            onClick={() => navigate('/users')}
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}

export default UserCreatePage