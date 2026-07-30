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

  useEffect(() => {
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

    load()
  }, [])

  return (
    <div>
      <h1>Karakterler</h1>
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