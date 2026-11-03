import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from './api'
import { hasPermission, PERMISSIONS } from './permissions'
import { usePermissions } from './PermissionContext'
import s from './UsersPage.module.scss'
import ListSearchBar from './ListSearchBar'

type UserRow = {
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
}

function UsersPage() {
  const permissions = usePermissions()
  const [items, setItems] = useState<UserRow[]>([])
  const [error, setError] = useState('')
  const [pageSize, setPageSize] = useState(20)
  const [searchDraft, setSearchDraft] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')

async function load() {
  try {
    const params = new URLSearchParams({
      page: '1',
      pageSize: String(pageSize),
    })
    const term = appliedSearch.trim()
    if (term) params.set('search', term)

    const response = await apiFetch(`/api/users?${params.toString()}`)

    if (!response.ok) {
      setError('Could not load users')
      return
    }

    const data = await response.json()
    setItems(data.items)
    setError('')
  } catch {
    setError('Could not reach API')
  }
}
useEffect(() => {
  load()
}, [pageSize, appliedSearch])
  // Eskiden yalnız [pageSize]: select değişince liste yenilenirdi; Characters’ta [] idi.
  // Şimdi appliedSearch de var: Search submit olunca aynı load yolu çalışır.
  // pageSize veya appliedSearch’ten BİRİ değişince effect yeniden koşar.
  // İkisi birden deps’te olduğu için “sayfa boyutu 50 + arama Dummy” birlikte tutarlı kalır.
  // searchDraft yine deps’te değil → yazarken GET yok.

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this user?')) return

    const response = await apiFetch(`/api/users/${id}`, {
      method: 'DELETE',
    })

    if (!response.ok) {
      setError('Could not delete user')
      return
    }

    await load()
  }

  return (
    <div className={s.page}>
      <div className={s.card}>
        <div className={s.header}>
          <h1>Users</h1>
          <div className={s.headerRight}>
            <ListSearchBar
              value={searchDraft}
              onChange={setSearchDraft}
              onSubmit={() => setAppliedSearch(searchDraft.trim())}
              placeholder="Search users…"
            />
            {hasPermission(permissions, PERMISSIONS.usersCreate) && (
              <Link className={s.btnAdd} to="/users/new">
                Add User
              </Link>
            )}
            <label className={s.pageSize}>
              Page size
              <select
                value={pageSize}
                onChange={(e) => setPageSize(Number(e.target.value))}
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
              </select>
            </label>
          </div>
        </div>

        {error && <p className={s.error}>{error}</p>}

        <table className={s.table}>
          <thead>
            <tr>
              <th>Avatar</th>
              <th>First Name</th>
              <th>Last Name</th>
              <th>Phone</th>
              <th>Email</th>
              <th>Role</th>
              <th>Disabled</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((u) => (
              <tr key={u.id}>
                <td>
                  {u.avatarUrl ? (
                    <img className={s.avatar} src={u.avatarUrl} alt="" />
                  ) : (
                    <span className={s.avatarEmpty} />
                  )}
                </td>
                <td>{u.firstName ?? '—'}</td>
                <td>{u.lastName ?? '—'}</td>
                <td>{u.phone ?? '—'}</td>
                <td>{u.email ?? '—'}</td>
                <td>{u.roles.length > 0 ? u.roles.join(', ') : '—'}</td>
                <td>{u.isDisabled ? 'Yes' : 'No'}</td>
                <td>
                  <div className={s.actions}>
                    <Link className={s.btnView} to={`/users/${u.id}`}>
                      View
                    </Link>
                    {hasPermission(permissions, PERMISSIONS.usersDelete) && (
                      <button
                        type="button"
                        className={s.btnDelete}
                        onClick={() => handleDelete(u.id)}
                      >
                        Delete
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default UsersPage