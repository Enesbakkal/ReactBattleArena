import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch } from './api'
import s from './TeamsPage.module.scss'

type TeamSlot = {
  slotIndex: number
  characterId: string | null
  characterName: string | null
  imageUrl: string | null
  baseAttack: number | null
  baseDefense: number | null
  baseSpeed: number | null
  baseHealth: number | null
  baseTechnique: number | null
  baseAccuracy: number | null
  baseForesight: number | null
}

type TeamRow = {
  id: string
  name: string
  createdAtUtc: string
  slots: TeamSlot[]
}

function formatPowers(slot: {
  baseAttack: number | null
  baseDefense: number | null
  baseSpeed: number | null
  baseHealth: number | null
  baseTechnique: number | null
  baseAccuracy: number | null
  baseForesight: number | null
}) {
  if (slot.baseAttack == null) return null
  return `AT ${slot.baseAttack} · DEF ${slot.baseDefense} · SPD ${slot.baseSpeed} · HP ${slot.baseHealth} · TECH ${slot.baseTechnique} · ACC ${slot.baseAccuracy} · FORE ${slot.baseForesight}`
}


function TeamsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<TeamRow[]>([])
  const [error, setError] = useState('')

  async function load() {
    try {
      const response = await apiFetch('/api/teams')
      if (!response.ok) {
        setError('Could not load teams')
        return
      }
      const data = await response.json()
      setItems(data)
      setError('')
    } catch {
      setError('Could not reach API')
    }
  }

  useEffect(() => {
    load()
  }, [])

  async function handleCreate() {
    setError('')
    const response = await apiFetch('/api/teams', {
      method: 'POST',
      body: { name: 'New Team' },
    })
    if (!response.ok) {
      setError('Could not create team')
      return
    }
    await load()
  }

  async function handleRename(id: string, name: string) {
    const trimmed = name.trim()
    if (!trimmed) return

    const response = await apiFetch(`/api/teams/${id}`, {
      method: 'PUT',
      body: { name: trimmed },
    })
    if (!response.ok) {
      setError('Could not rename team')
      return
    }
    await load()
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this team?')) return

    const response = await apiFetch(`/api/teams/${id}`, {
      method: 'DELETE',
    })
    if (!response.ok) {
      setError('Could not delete team')
      return
    }
    await load()
  }

  return (
    <div className={s.page}>
      <div className={s.card}>
        <div className={s.header}>
          <h1>My Teams</h1>
          <button type="button" className={s.btnAdd} onClick={handleCreate}>
            Add Team
          </button>
        </div>

        {error && <p className={s.error}>{error}</p>}

        <div className={s.list}>
          {items.map((team) => (
            <div
              key={team.id}
              className={s.row}
              onClick={() => navigate(`/teams/${team.id}`)}
            >
              <input
                className={s.nameInput}
                defaultValue={team.name}
                onClick={(e) => e.stopPropagation()}
                onBlur={(e) => {
                  if (e.target.value.trim() !== team.name) {
                    void handleRename(team.id, e.target.value)
                  }
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.currentTarget.blur()
                  }
                }}
              />

              <div className={s.slots}>
                {team.slots.map((slot) => (
                  <div key={slot.slotIndex} className={s.slot}>
                    <div>{slot.characterName ?? 'Empty'}</div>
                    {formatPowers(slot) && (
                      <div className={s.powers}>{formatPowers(slot)}</div>
                    )}
                  </div>
                ))}
              </div>

              <button
                type="button"
                className={s.btnDelete}
                onClick={(e) => {
                  e.stopPropagation()
                  void handleDelete(team.id)
                }}
              >
                Delete
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default TeamsPage

// Birkaç nokta önemli. GET /api/teams dizi döner (data.items değil); Users’taki paging zarfı burada yok. Ad input’unda stopPropagation olmazsa tıklayınca hemen düzenleme rotasına gider — 
// sayfa modeli bunu istemiyor. onBlur ile PUT atılır; Enter blur tetikler. Satırın geri kalanına tıklanınca navigate; 
// listede Add / rename / delete / 5 slot görünmesi bu parçanın hedefi.
// defaultValue kullanıyoruz çünkü her load sonrası input’u sunucu adıyla yenilemek için satır yeniden çizilir; controlled value + her tuşta state de olurdu, blur-rename için defaultValue yeterli.
// ASP.NET eşlemesi: Add → POST, blur → PUT, Delete → DELETE, açılış → GET; hepsi JWT’deki kullanıcıya ait (OwnerUserId).
// DOM’da blur, input’un odakı kaybetmesi demektir: kutunun içindeyken dışarı tıklarsın, Tab ile çıkarsın veya Enter ile blur() çağırırsın. React’teki onBlur o an çalışır. CSS filter: blur(...) ile ilgisi yok; sayfa görsel olarak sislenmez.

// Bizde akış şöyle: adı yazıyorsun → kutudan çıkıyorsun → onBlur → ad değiştiyse PUT /api/teams/{id} → load() ile liste yenilenir. Görmen gereken şey sis değil; kaydedilmiş yeni ad (ve Network’te o PUT).

// Denemek için: adı değiştir, input’un dışına tıkla (veya Enter’a bas). Ad yerinde kalıyorsa ve Scalar/Network’te PUT 204 ise blur doğru çalışıyordur.