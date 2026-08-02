import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AppLayout() {
  const { user, logout } = useAuth()

  return (
    <>
      <nav className="container-fluid">
        <ul>
          <li>
            <strong>Household Budget</strong>
          </li>
        </ul>
        <ul>
          <li>
            <NavLink to="/">Home</NavLink>
          </li>
          <li>
            <NavLink to="/accounts">Accounts</NavLink>
          </li>
          <li>
            <NavLink to="/categories">Categories</NavLink>
          </li>
          <li>
            <NavLink to="/transactions">Transactions</NavLink>
          </li>
          <li>
            <NavLink to="/budgets">Budgets</NavLink>
          </li>
        </ul>
        <ul>
          <li>Signed in as {user?.displayName}</li>
          <li>
            <button className="secondary" onClick={() => void logout()}>
              Log out
            </button>
          </li>
        </ul>
      </nav>
      <main className="container">
        <Outlet />
      </main>
    </>
  )
}
