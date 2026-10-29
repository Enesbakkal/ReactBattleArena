import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from './api'
import s from './EditProfilePage.module.scss'

function ChangePasswordPage() {
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess(false)

    if (newPassword !== confirmPassword) {
      setError('New password and confirmation do not match')
      return
    }

    setSaving(true)
    try {
      const response = await apiFetch('/api/users/password', {
        method: 'PUT',
        body: {
          currentPassword,
          newPassword,
          confirmPassword,
        },
      })

      if (!response.ok) {
        setError(
          response.status === 400
            ? 'Current password is wrong or new password is invalid'
            : 'Could not change password',
        )
        return
      }

      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSuccess(true)
    } catch {
      setError('Could not reach API')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={s.page}>
      <h1>Change Password</h1>
      {error && <p className={s.error}>{error}</p>}
      {success && <p className={s.success}>Password changed</p>}

      <form className={s.form} onSubmit={handleSubmit}>
        <label className={s.field}>
          Current Password
          <input
            type="password"
            required
            autoComplete="current-password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
        </label>

        <label className={s.field}>
          New Password
          <input
            type="password"
            required
            minLength={6}
            autoComplete="new-password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
        </label>

        <label className={s.field}>
          Confirm New Password
          <input
            type="password"
            required
            minLength={6}
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
        </label>

        <div className={s.actions}>
          <button className={s.save} type="submit" disabled={saving}>
            Change Password
          </button>
          <button
            className={s.secondary}
            type="button"
            disabled={saving}
            onClick={() => navigate('/profile')}
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}

export default ChangePasswordPage