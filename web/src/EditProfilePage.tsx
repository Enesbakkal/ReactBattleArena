import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from './api'
import { useUserId } from './PermissionContext'
import s from './EditProfilePage.module.scss'

type ProfileForm = {
  firstName: string
  lastName: string
  phone: string
  email: string
  avatarUrl: string
  isContactPublic: boolean
}

const emptyForm: ProfileForm = {
  firstName: '',
  lastName: '',
  phone: '',
  email: '',
  avatarUrl: '',
  isContactPublic: true,
}

function EditProfilePage() {
  const userId = useUserId()
  const navigate = useNavigate()

  const [form, setForm] = useState<ProfileForm>(emptyForm)
  const [snapshot, setSnapshot] = useState<ProfileForm>(emptyForm)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!userId) {
      setLoading(false)
      return
    }

    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const response = await apiFetch(`/api/users/${userId}`)
        if (!response.ok) {
          if (!cancelled) setError('Could not load profile')
          return
        }
        const data = await response.json()
        const next: ProfileForm = {
          firstName: data.firstName ?? '',
          lastName: data.lastName ?? '',
          phone: data.phone ?? '',
          email: data.email ?? '',
          avatarUrl: data.avatarUrl ?? '',
          isContactPublic: data.isContactPublic === true,
        }
        if (!cancelled) {
          setForm(next)
          setSnapshot(next)
        }
      } catch {
        if (!cancelled) setError('Could not reach API')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [userId])

  function updateField<K extends keyof ProfileForm>(key: K, value: ProfileForm[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function handleReset() {
    setForm(snapshot)
    setError('')
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError('')

    try {
      const response = await apiFetch('/api/users/profile', {
        method: 'PUT',
        body: {
          firstName: form.firstName.trim() || null,
          lastName: form.lastName.trim() || null,
          phone: form.phone.trim() || null,
          email: form.email.trim(),
          avatarUrl: form.avatarUrl.trim() || null,
          isContactPublic: form.isContactPublic,
        },
      })

      if (!response.ok) {
        setError(response.status === 400 ? 'Validation failed' : 'Could not save profile')
        return
      }

      setSnapshot(form)
      navigate('/profile')
    } catch {
      setError('Could not reach API')
    } finally {
      setSaving(false)
    }
  }

  if (!userId) return <p>No session</p>
  if (loading) return <p>Loading…</p>

  return (
    <div className={s.page}>
      <h1>Edit Profile</h1>
      {error && <p className={s.error}>{error}</p>}

      <form className={s.form} onSubmit={handleSubmit}>
        <label className={s.field}>
          First Name
          <input
            value={form.firstName}
            onChange={(e) => updateField('firstName', e.target.value)}
          />
        </label>

        <label className={s.field}>
          Last Name
          <input
            value={form.lastName}
            onChange={(e) => updateField('lastName', e.target.value)}
          />
        </label>

        <label className={s.field}>
          Phone
          <input
            value={form.phone}
            onChange={(e) => updateField('phone', e.target.value)}
          />
        </label>

        <label className={s.field}>
          Email
          <input
            type="email"
            required
            value={form.email}
            onChange={(e) => updateField('email', e.target.value)}
          />
        </label>

        <label className={s.field}>
          Avatar URL
          <input
            value={form.avatarUrl}
            onChange={(e) => updateField('avatarUrl', e.target.value)}
          />
        </label>

        <label className={s.check}>
          <input
            type="checkbox"
            checked={form.isContactPublic}
            onChange={(e) => updateField('isContactPublic', e.target.checked)}
          />
          Make email and phone public
        </label>

        <div className={s.actions}>
          <button className={s.save} type="submit" disabled={saving}>
            Save
          </button>
          <button
            className={s.secondary}
            type="button"
            disabled={saving}
            onClick={handleReset}
          >
            Reset
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

export default EditProfilePage