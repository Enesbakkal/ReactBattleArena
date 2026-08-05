import { useEffect, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import './CharactersPage.css'

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
  const token = localStorage.getItem('token')

  const [character, setCharacter] = useState<CharacterDetail | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  if (!token) {
    return <Navigate to="/login" replace />
  }

  useEffect(() => {
    async function load() {
      if (!id) {
        setError('Id yok')
        setLoading(false)
        return
      }

      setLoading(true)
      setError('')

      try {
        const response = await fetch(
          `https://localhost:7275/api/characters/${id}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          },
        )

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

  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakter detay</h1>
        <Link to="/characters">Listeye dön</Link>
      </div>

      {loading && <p>Yükleniyor…</p>}
      {error && <p>{error}</p>}

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