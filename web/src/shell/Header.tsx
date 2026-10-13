import BurgerIcon from './BurgerIcon'
import PowerIcon from './PowerIcon'
import s from './Header.module.scss'

type HeaderProps = {
  onToggleSidebar: () => void
  onLogout: () => void
}

function Header({ onToggleSidebar, onLogout }: HeaderProps) {
  return (
    <header className={s.root}>
      <button
        type="button"
        className={`${s.iconButton} ${s.burger}`}
        onClick={onToggleSidebar}
        aria-label="Menü"
      >
        <BurgerIcon />
      </button>
      <button
        type="button"
        className={s.iconButton}
        onClick={onLogout}
        aria-label="Çıkış"
      >
        <PowerIcon />
      </button>
    </header>
  )
}

export default Header