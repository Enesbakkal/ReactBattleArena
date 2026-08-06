import { useEffect, useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import './CharactersPage.css'

function CharacterEditPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const token = localStorage.getItem('token')

  const [name, setName] = useState('')
  const [universe, setUniverse] = useState('')
  const [biography, setBiography] = useState('')
  const [rarity, setRarity] = useState(1)
  const [baseAttack, setBaseAttack] = useState(10)
  const [baseDefense, setBaseDefense] = useState(10)
  const [baseSpeed, setBaseSpeed] = useState(10)
  const [imageUrl, setImageUrl] = useState('')

  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')

  useEffect(() => {
    async function load() {
      if (!token || !id) {
        setLoadError('Id veya token yok')
        setLoading(false)
        return
      }

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
          setLoadError('Karakter bulunamadı')
          setLoading(false)
          return
        }

        if (!response.ok) {
          setLoadError('Karakter alınamadı')
          setLoading(false)
          return
        }

        const data = await response.json()
        setName(data.name)
        setUniverse(data.universe)
        setBiography(data.biography ?? '')
        setRarity(data.rarity)
        setBaseAttack(data.baseAttack)
        setBaseDefense(data.baseDefense)
        setBaseSpeed(data.baseSpeed)
        setImageUrl(data.imageUrl ?? '')
      } catch {
        setLoadError('API’ye ulaşılamadı')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [id, token])

  if (!token) {
    return <Navigate to="/login" replace />
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError('')

    try {
      const response = await fetch(
        `https://localhost:7275/api/characters/${id}`,
        {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            name,
            universe,
            biography: biography || null,
            rarity,
            baseAttack,
            baseDefense,
            baseSpeed,
            imageUrl: imageUrl || null,
          }),
        },
      )

      if (response.status === 401) {
        setFormError('Oturum yok — tekrar giriş yap')
        return
      }

      if (response.status === 403) {
        setFormError('Yetkin yok (Admin gerekli)')
        return
      }

      if (response.status === 404) {
        setFormError('Karakter bulunamadı')
        return
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null)
        const messages = problem?.errors
          ? Object.values(problem.errors).flat().join(' | ')
          : problem?.title ?? `Hata ${response.status}`
        setFormError(String(messages))
        return
      }

      // 204 No Content — body yok; json() çağırma
      navigate(`/characters/${id}`)
    } catch {
      setFormError('API’ye ulaşılamadı')
    }
  }

  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakter düzenle</h1>
        <Link to={`/characters/${id}`}>Detaya dön</Link>
      </div>

      {loading && <p>Yükleniyor…</p>}
      {loadError && <p>{loadError}</p>}

      {!loading && !loadError && (
        <form className="characters-form" onSubmit={handleSubmit}>
          <div>
            <label>
              Ad
              <input value={name} onChange={(e) => setName(e.target.value)} />
            </label>
          </div>
          <div>
            <label>
              Evren
              <input
                value={universe}
                onChange={(e) => setUniverse(e.target.value)}
              />
            </label>
          </div>
          <div>
            <label>
              Biyografi
              <input
                value={biography}
                onChange={(e) => setBiography(e.target.value)}
              />
            </label>
          </div>
          <div>
            <label>
              Rarity
              <input
                type="number"
                value={rarity}
                onChange={(e) => setRarity(Number(e.target.value))}
              />
            </label>
          </div>
          <div>
            <label>
              Attack
              <input
                type="number"
                value={baseAttack}
                onChange={(e) => setBaseAttack(Number(e.target.value))}
              />
            </label>
          </div>
          <div>
            <label>
              Defense
              <input
                type="number"
                value={baseDefense}
                onChange={(e) => setBaseDefense(Number(e.target.value))}
              />
            </label>
          </div>
          <div>
            <label>
              Speed
              <input
                type="number"
                value={baseSpeed}
                onChange={(e) => setBaseSpeed(Number(e.target.value))}
              />
            </label>
          </div>
          <div>
            <label>
              Image URL
              <input
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
              />
            </label>
          </div>
          <button type="submit">Kaydet</button>
          {formError && <p>{formError}</p>}
        </form>
      )}
    </div>
  )
}

export default CharacterEditPage