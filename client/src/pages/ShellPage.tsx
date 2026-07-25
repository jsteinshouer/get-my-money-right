import { useAuth } from '../auth/AuthContext'

export function ShellPage() {
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
          <li>Signed in as {user?.displayName}</li>
          <li>
            <button className="secondary" onClick={() => void logout()}>
              Log out
            </button>
          </li>
        </ul>
      </nav>
      <main className="container">
        <h1>Welcome, {user?.displayName}</h1>
        <p>You're signed in and viewing the household budget shell.</p>
      </main>
    </>
  )
}
