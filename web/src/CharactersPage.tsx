import { useEffect, useState } from 'react'
// import { Link, Navigate, useNavigate } from 'react-router-dom' Navigate ve  useNAvigate AppLayoutdan yapılacağı için sildik
import { Link } from 'react-router-dom'
import CharacterCard from './CharacterCard'
import './CharactersPage.css'
import { apiFetch } from './api'
import {hasPermission, PERMISSIONS } from './permissions'
import { usePermissions } from './PermissionContext'

interface CharacterRow {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharactersPage() {
  const permissions = usePermissions()
  const [items, setItems] = useState<CharacterRow[]>([])
  // const [permissions, setPermissions] = useState<string[]>([])   bu satırı sildik çünkü permission ve me ile authorization app layoutden yapmaya karar verdik 
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

      // const meResponse = await apiFetch('/api/auth/me')
      // if(meResponse.ok){
      //   const me = await meResponse.json()
      //   setPermissions(me.permissions ?? [])
      // }  
      // buraya artık gerek kalmadı çünkü me authorization'ı artık applayoutdan yapmaya karar verdik

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
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
            {/* Evet: charactersCreate (yani "characters.create") listede varsa Karakter ekle çizilir. Alttakiler de aynı fikir: characters.update varsa Düzenle, characters.delete varsa Sil.
            &&’in solundaki koşul, sağındaki Link değil. JavaScript şöyle çalışır: A && B — A yanlışsa B’ye hiç bakılmaz, sonuç false olur; A doğruysa sonuç B’dir. 
            React bunu JSX’te görünce false ise hiçbir şey basmaz, true ise sağdaki <Link> veya <button>’u basar. Yani hasPermission(...) kapı, Link sadece “kapı açıksa çizilecek parça.” */}
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