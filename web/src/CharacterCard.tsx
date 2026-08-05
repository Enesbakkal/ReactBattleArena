import { Link } from 'react-router-dom'

interface CharacterCardProps {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
        <article className="character-card">
              {imageUrl ? (
                <img src={imageUrl} alt={name} className="character-card__image" />
              ) : (
                <div className="character-card__placeholder">No image</div>
              )}
              <h3 className="character-card__name">{name}</h3>
              <p className="character-card__meta">{universe}</p>
              <p className="character-card__meta">Rarity {rarity}</p>
            </article>
      </Link>
  )
}

export default CharacterCard