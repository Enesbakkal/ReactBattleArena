interface CharacterCardProps {
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharacterCard({ name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
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
  )
}

export default CharacterCard