import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const LINKS = [
  { to: '/', label: 'This month', end: true },
  { to: '/transactions', label: 'Transactions', end: false },
  { to: '/budgets', label: 'Budgets', end: false },
  { to: '/categories', label: 'Categories', end: false },
  { to: '/accounts', label: 'Accounts', end: false },
]

export function AppLayout() {
  const { user, logout } = useAuth()

  return (
    <>
      <header className="masthead">
        <div className="masthead-inner">
          <NavLink className="wordmark" to="/">
            Money<span>Right</span>
          </NavLink>
          <nav aria-label="Sections">
            {LINKS.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) => (isActive ? 'active' : undefined)}
              >
                {link.label}
              </NavLink>
            ))}
          </nav>
          <div className="masthead-user">
            <span>{user?.displayName}</span>
            <button className="secondary" onClick={() => void logout()}>
              Log out
            </button>
          </div>
        </div>
      </header>
      <main className="container">
        <Outlet />
      </main>
    </>
  )
}
