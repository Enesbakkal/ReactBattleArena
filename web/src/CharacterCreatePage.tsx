import { useEffect, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import CharacterCard from './CharacterCard'
import './CharactersPage.css'
import { apiFetch, getToken } from './api'

interface CharacterRow {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharacterCreatePage() {
  const navigate = useNavigate()
  const token = getToken()

  const [name, setName] = useState('')
  const [universe, setUniverse] = useState('')
  const [biography, setBiography] = useState('')
  const [rarity, setRarity] = useState(1)
  const [baseAttack, setBaseAttack] = useState(10)
  const [baseDefense, setBaseDefense] = useState(10)
  const [baseSpeed, setBaseSpeed] = useState(10)
  const [imageUrl, setImageUrl] = useState('')
  const [formError, setFormError] = useState('')
  const [formSuccess, setFormSuccess] = useState('')

  const [items, setItems] = useState<CharacterRow[]>([])

  if (!token) {
    return <Navigate to="/login" replace />
  }

  async function loadPreview() {
    try {
      // const response = await fetch(
      //   'https://localhost:7275/api/characters?page=1&pageSize=8',
      //   {
      //     headers: {
      //       Authorization: `Bearer ${token}`,
      //     },
      //   },
      // )   Burasına gerek kalmadı çünkü ortak auth yazdık

      const response = await apiFetch('/api/characters?page=1&pageSize=8')

      if (!response.ok) return
      const data = await response.json()
      setItems(data.items)
    } catch {
      // önizleme opsiyonel; formu bozma
    }
  }

  useEffect(() => {
    loadPreview()
  }, [])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setFormError('')
    setFormSuccess('')

    try {
      // const response = await fetch('https://localhost:7275/api/characters', {
      //   method: 'POST',
      //   headers: {
      //     'Content-Type': 'application/json',
      //     Authorization: `Bearer ${token}`,
      //   },
      //   body: JSON.stringify({
      //     name,
      //     universe,
      //     biography: biography || null,
      //     rarity,
      //     baseAttack,
      //     baseDefense,
      //     baseSpeed,
      //     imageUrl: imageUrl || null,
      //   }),
      // })    buraya gerek kalmadı çünkü ortak auth yazdık

      const response = await apiFetch('/api/characters', {
        method: 'POST',
        body: {
          name,
          universe,
          biography: biography || null,
          rarity,
          baseAttack,
          baseDefense,
          baseSpeed,
          imageUrl: imageUrl || null,
        },
      })

      if (response.status === 403) {
        setFormError('Yetkin yok (Admin gerekli)')
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

      setFormSuccess('Karakter eklendi')
      setName('')
      setUniverse('')
      setBiography('')
      setImageUrl('')
      await loadPreview()
      navigate('/characters')
    } catch {
      setFormError('API’ye ulaşılamadı')
    }
  }

  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakter ekle</h1>
        <Link to="/characters">Listeye dön</Link>
      </div>

      <form className="characters-form" onSubmit={handleCreate}>
        <h2>Yeni karakter (Admin)</h2>
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
        <button type="submit">Ekle</button>
        {formError && <p>{formError}</p>}
        {formSuccess && <p>{formSuccess}</p>}
      </form>

      <h2>Mevcut karakterler</h2>
      <p className="characters-hint">
        Şimdilik önizleme (sık kullanılanlar backend sonra). Aynı kart grid.
      </p>
      <div className="characters-grid">
        {items.map((c) => (
          <CharacterCard
            key={c.id}
            id={c.id}
            name={c.name}
            universe={c.universe}
            rarity={c.rarity}
            imageUrl={c.imageUrl}
          />
        ))}
      </div>
    </div>
  )
}

export default CharacterCreatePage