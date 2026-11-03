import s from './ListSearchBar.module.scss'

type ListSearchBarProps = {
  value: string
  onChange: (value: string) => void
  onSubmit: () => void
  placeholder?: string
}

function ListSearchBar({
  value,
  onChange,
  onSubmit,
  placeholder = 'Search…',
}: ListSearchBarProps) {
  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    onSubmit()
  }

  return (
    <form className={s.form} onSubmit={handleSubmit}>
      <input
        className={s.input}
        type="search"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        aria-label="Search"
      />
      <button className={s.button} type="submit">
        Search
      </button>
    </form>
  )
}

export default ListSearchBar