import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch } from './api'
import ListSearchBar from './ListSearchBar'
import s from './TeamEditPage.module.scss'

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

type TeamDetail = {
  id: string
  name: string
  createdAtUtc: string
  slots: TeamSlot[]
}

type CharacterHit = {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
  baseAttack: number
  baseDefense: number
  baseSpeed: number
  baseHealth: number
  baseTechnique: number
  baseAccuracy: number
  baseForesight: number
}

function formatPowers(p: {
  baseAttack?: number | null
  baseDefense?: number | null
  baseSpeed?: number | null
  baseHealth?: number | null
  baseTechnique?: number | null
  baseAccuracy?: number | null
  baseForesight?: number | null
}) {
  if (p.baseAttack == null) return null
  return `AT ${p.baseAttack} · DEF ${p.baseDefense} · SPD ${p.baseSpeed} · HP ${p.baseHealth} · TECH ${p.baseTechnique} · ACC ${p.baseAccuracy} · FORE ${p.baseForesight}`
}

function TeamEditPage() {
  const { id } = useParams<{ id: string }>()
  const [team, setTeam] = useState<TeamDetail | null>(null)
  const [error, setError] = useState('')
  const [selectedSlot, setSelectedSlot] = useState<number | null>(null)

  const [searchDraft, setSearchDraft] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [hits, setHits] = useState<CharacterHit[]>([])

  async function loadTeam() {
    if (!id) return
    try {
      const response = await apiFetch(`/api/teams/${id}`)
      if (response.status === 404) {
        setError('Team not found')
        setTeam(null)
        return
      }
      if (!response.ok) {
        setError('Could not load team')
        return
      }
      const data = await response.json()
      setTeam(data)
      setError('')
    } catch {
      setError('Could not reach API')
    }
  }

  async function loadCatalog() {
    try {
      const params = new URLSearchParams({
        page: '1',
        pageSize: '20',
      })
      const term = appliedSearch.trim()
      if (term) params.set('search', term)

      const response = await apiFetch(`/api/characters?${params.toString()}`)
      if (!response.ok) {
        setError('Could not load characters')
        return
      }
      const data = await response.json()
      setHits(data.items)
    } catch {
      setError('Could not reach API')
    }
  }

  useEffect(() => {
    void loadTeam()
  }, [id])

  useEffect(() => {
    void loadCatalog()
  }, [appliedSearch])

  async function handleAdd(characterId: string) {
    if (!id || selectedSlot === null) {
      setError('Select an empty slot first')
      return
    }

    setError('')
    const response = await apiFetch(`/api/teams/${id}/members`, {
      method: 'POST',
      body: { characterId, slotIndex: selectedSlot },
    })

    if (response.status === 400) {
      const text = await response.text()
      setError(text || 'Could not add member')
      return
    }
    if (!response.ok) {
      setError('Could not add member')
      return
    }

    setSelectedSlot(null)
    await loadTeam()
  }

  async function handleRemove(slotIndex: number) {
    if (!id) return
    setError('')
    const response = await apiFetch(`/api/teams/${id}/members/${slotIndex}`, {
      method: 'DELETE',
    })
    if (!response.ok) {
      setError('Could not remove member')
      return
    }
    if (selectedSlot === slotIndex) setSelectedSlot(null)
    await loadTeam()
  }

  if (!team && !error) {
    return <p>Loading…</p>
  }

  return (
    <div className={s.page}>
      <div className={s.card}>
        <div className={s.header}>
          <h1>{team?.name ?? 'Team'}</h1>
          <Link className={s.back} to="/teams">
            Back to My Teams
          </Link>
        </div>

        {error && <p className={s.error}>{error}</p>}

        <h2 className={s.sectionTitle}>Slots</h2>
        <p className={s.hint}>Click an empty slot, then Add on a character below.</p>
        <div className={s.slots}>
          {team?.slots.map((slot) => {
            const filled = slot.characterId != null
            const selected = selectedSlot === slot.slotIndex
            return (
              <div
                key={slot.slotIndex}
                className={`${s.slot} ${filled ? s.slotFilled : ''} ${selected ? s.slotSelected : ''}`}
                onClick={() => {
                  if (!filled) setSelectedSlot(slot.slotIndex)
                }}
              >
                <div className={s.slotIndex}>#{slot.slotIndex}</div>
                    <div className={s.slotName}>
                    {slot.characterName ?? 'Empty'}
                    </div>
                    {formatPowers(slot) && (
                    <div className={s.powers}>{formatPowers(slot)}</div>
                    )}
                    {filled && (
                  <button
                    type="button"
                    className={s.btnRemove}
                    onClick={(e) => {
                      e.stopPropagation()
                      void handleRemove(slot.slotIndex)
                    }}
                  >
                    Remove
                  </button>
                )}
              </div>
            )
          })}
        </div>

        <h2 className={s.sectionTitle}>Catalog</h2>
        <ListSearchBar
          value={searchDraft}
          onChange={setSearchDraft}
          onSubmit={() => setAppliedSearch(searchDraft.trim())}
          placeholder="Search name or universe…"
        />

        <div className={s.catalog}>
          {hits.map((c) => (
            <div key={c.id} className={s.hit}>
              <div>
                <strong>{c.name}</strong>
                <span className={s.meta}>
                    {c.universe} · R{c.rarity}
                </span>
                {formatPowers(c) && (
                    <span className={s.powers}>{formatPowers(c)}</span>
                )}
              </div>
              <button
                type="button"
                className={s.btnAdd}
                onClick={() => void handleAdd(c.id)}
              >
                Add
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default TeamEditPage

// Üst blok takımı GET /api/teams/{id} ile doldurur — ASP.NET’te GetById + OwnerUserId. Boş kutuya tıklayınca selectedSlot seçilir; dolu kutuda Remove DELETE .../members/{slotIndex} atar. 
// Alt katalog Characters listesindeki gibi search query kullanır; Create Character bu sayfada yok. Add, seçili slot + karakter id ile POST .../members gönderir; 400 metni (slot dolu / zaten takımda) ekrana düşer.

// İki useEffect var: biri id değişince takımı, biri appliedSearch değişince kataloğu çeker. Search yine 1A (draft / applied); her tuşta istek yok.