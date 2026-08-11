import { useEffect, useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import './CharactersPage.css'
import { apiFetch, getToken } from './api'

interface CharacterDetail {
  id: string
  name: string
  universe: string
  biography?: string | null
  rarity: number
  baseAttack: number
  baseDefense: number
  baseSpeed: number
  imageUrl?: string | null
  createdAtUtc: string
}

function CharacterDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  // const token = localStorage.getItem('token') ortak auth 
  const token = getToken()

  const [character, setCharacter] = useState<CharacterDetail | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [deleteError, setDeleteError] = useState('')

  useEffect(() => {
    async function load() {
      if (!token || !id) {
        setError('Id veya token yok')
        setLoading(false)
        return
      }

      setLoading(true)
      setError('')

      try {
        // const response = await fetch(
        //   `https://localhost:7275/api/characters/${id}`,
        //   {
        //     headers: {
        //       Authorization: `Bearer ${token}`,
        //     },
        //   },
        // ) Ortak auth

        const response = await apiFetch(`/api/characters/${id}`)

        if (response.status === 404) {
          setError('Karakter bulunamadı')
          setCharacter(null)
          setLoading(false)
          return
        }

        if (!response.ok) {
          setError('Karakter alınamadı')
          setLoading(false)
          return
        }

        const data = await response.json()
        setCharacter(data)
      } catch {
        setError('API’ye ulaşılamadı')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [id, token])

  if (!token) {
    return <Navigate to="/login" replace />
  }

  async function handleDelete() {
    if (!id || !token) return

    const ok = window.confirm('Bu karakteri silmek istediğine emin misin?')
    if (!ok) return

    setDeleteError('')

    try {
      // const response = await fetch(
      //   `https://localhost:7275/api/characters/${id}`,
      //   {
      //     method: 'DELETE',
      //     headers: {
      //       Authorization: `Bearer ${token}`,
      //     },
      //   },
      // )

      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'DELETE',
      })

      if (response.status === 401) {
        setDeleteError('Oturum yok — tekrar giriş yap')
        return
      }

      if (response.status === 403) {
        setDeleteError('Yetkin yok (Admin gerekli)')
        return
      }

      if (response.status === 404) {
        setDeleteError('Karakter bulunamadı')
        return
      }

      if (!response.ok) {
        setDeleteError(`Silinemedi (${response.status})`)
        return
      }

      navigate('/characters')
    } catch {
      setDeleteError('API’ye ulaşılamadı')
    }
  }

  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakter detay</h1>
        <div className="characters-page__actions">
          <Link to={`/characters/${id}/edit`}>Düzenle</Link>
          <button type="button" onClick={handleDelete}>
            Sil
          </button>
          <Link to="/characters">Listeye dön</Link>
        </div>
      </div>

      {loading && <p>Yükleniyor…</p>}
      {error && <p>{error}</p>}
      {deleteError && <p>{deleteError}</p>}

      {character && (
        <article className="character-detail">
          {character.imageUrl ? (
            <img
              src={character.imageUrl}
              alt={character.name}
              className="character-detail__image"
            />
          ) : (
            <div className="character-card__placeholder">No image</div>
          )}
          <h2>{character.name}</h2>
          <p>{character.universe}</p>
          <p>Rarity {character.rarity}</p>
          <p>
            AT {character.baseAttack} · DEF {character.baseDefense} · SPD{' '}
            {character.baseSpeed}
          </p>
          {character.biography && <p>{character.biography}</p>}
          <p className="characters-hint">
            Oluşturulma: {new Date(character.createdAtUtc).toLocaleString()}
          </p>
        </article>
      )}
    </div>
  )
}

export default CharacterDetailPage
