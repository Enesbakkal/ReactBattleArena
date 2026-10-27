import { NavLink } from 'react-router-dom'
import s from './Sidebar.module.scss'

type SidebarProps = {
  open: boolean
}

function Sidebar({ open }: SidebarProps) {
  return (
    <aside className={open ? s.root : `${s.root} ${s.closed}`}>
      <div className={s.logo}>
        <NavLink to="/characters">Battle Arena</NavLink>
      </div>
      <h5 className={s.navTitle}>APP</h5>
      <ul className={s.nav}>
        <li>
          <NavLink
            to="/characters"
            className={({ isActive }) => (isActive ? `${s.link} ${s.active}` : s.link)}
          >
            Karakterler
          </NavLink>
        </li>
      </ul>
      <h5 className={s.navTitle}>USERS</h5>
      <ul className={s.nav}>
        <li>
          <NavLink
            to="/users"
            className={({ isActive }) => (isActive ? `${s.link} ${s.active}` : s.link)}
          >
            User Management
          </NavLink>
        </li>
        <li>
          <NavLink
            to="/profile"
            className={({ isActive }) => (isActive ? `${s.link} ${s.active}` : s.link)}
          >
            My Profile
          </NavLink>
        </li>
        <li>
          <NavLink
            to="/profile/edit"
            className={({ isActive }) => (isActive ? `${s.link} ${s.active}` : s.link)}
          >
            Edit Profile
          </NavLink>
        </li>
        <li>
          <NavLink
            to="/profile/password"
            className={({ isActive }) => (isActive ? `${s.link} ${s.active}` : s.link)}
          >
            Change Password
          </NavLink>
        </li>
      </ul>
    </aside>
  )
}

export default Sidebar