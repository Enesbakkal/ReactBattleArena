import { useEffect, useState } from 'react'
// import { Link, Navigate, useNavigate } from 'react-router-dom' Navigate ve  useNAvigate AppLayoutdan yapılacağı için sildik
import { Link } from 'react-router-dom'
import CharacterCard from './CharacterCard'
import './CharactersPage.css'
import { apiFetch } from './api'

interface CharacterRow {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharactersPage() {
  const [items, setItems] = useState<CharacterRow[]>([])
  const [error, setError] = useState('')
  // const navigate = useNavigate()
 
 
  // const token = localStorage.getItem('token')  (bu sayfada artık lazım değil)  cünkü ortak apı uth metodları yazdık

  // if (!token) {
  //   return <Navigate to="/login" replace />
  // }

  // function handleLogout() {
  //   localStorage.removeItem('token')
  //   navigate('/login')
  // yeni AppLayout eklediğimiz için buradan kaldırdık
  // }

  // async function load() { (bu sayfada artık lazım değil)  cünkü ortak apı uth metodları yazdık

  //   try {
  //     const response = await fetch(
  //       'https://localhost:7275/api/characters?page=1&pageSize=20',
  //       {
  //         headers: {
  //           Authorization: `Bearer ${token}`,
  //         },
  //       },
  //     )

  //     if (!response.ok) {
  //       setError('Karakterler alınamadı')
  //       return
  //     }

  //     const data = await response.json()
  //     setItems(data.items)
  //   } catch {
  //     setError('API’ye ulaşılamadı')
  //   }
  // }

  async function load() {
    try{
      const response = await apiFetch('/api/characters?page=1&pageSize=20')

      if(!response.ok) {
        setError('Karakterler Alınmadı')
        return
      }

      const data = await response.json()
      setItems(data.items)
    }catch {
      setError('API’ye ulaşılamadı')
    }  
  }

  useEffect(() => {
    load()
  }, [])

  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakterler</h1>
        <div className="characters-page__actions">
          <Link to="/characters/new">Karakter ekle</Link>
          {/* <button type="button" onClick={handleLogout}>
            Çıkış
          </button> */}
        </div>
      </div>

      {error && <p>{error}</p>}
      <div className="characters-grid">
        {items.map((c) => (
          <CharacterCard
            key={c.id}
            id= {c.id}
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

export default CharactersPage