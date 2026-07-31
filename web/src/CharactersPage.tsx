import { useEffect, useState } from 'react'

interface CharacterRow {
  id: string
  name: string
  universe: string
  rarity: number
}

function CharactersPage() {
  const [items, setItems] = useState<CharacterRow[]>([])
  const [error, setError] = useState('')

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

  async function load() {
    const token = localStorage.getItem('token')
    if (!token) {
      setError('Token yok — önce login ol')
      return
    }

    try {
      const response = await fetch(
        'https://localhost:7275/api/characters?page=1&pageSize=20',
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        },
      )

      if (!response.ok) {
        setError('Karakterler alınamadı')
        return
      }

      const data = await response.json()
      setItems(data.items)
    } catch {
      setError('API’ye ulaşılamadı')
    }
  }

  useEffect(() => {
    load()
  }, [])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setFormError('')
    setFormSuccess('')

    const token = localStorage.getItem('token')
    if (!token) {
      setFormError('Token yok')
      return
    }

    try {
      const response = await fetch('https://localhost:7275/api/characters', {
        method: 'POST',
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
      })

      if (response.status === 403) {
        setFormError('Yetkin yok (Admin gerekli)')
        return
      }

      if (!response.ok) {
        setFormError('Karakter eklenemedi (validation?)')
        return
      }

      setFormSuccess('Karakter eklendi')
      setName('')
      setUniverse('')
      setBiography('')
      setImageUrl('')
      await load()
    } catch {
      setFormError('API’ye ulaşılamadı')
    }
  }

  return (
    <div>
      <h1>Karakterler</h1>

      <form onSubmit={handleCreate}>
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

      {error && <p>{error}</p>}
      <ul>
        {items.map((c) => (
          <li key={c.id}>
            {c.name} — {c.universe} (rarity {c.rarity})
          </li>
        ))}
      </ul>
    </div>
  )
}

export default CharactersPage