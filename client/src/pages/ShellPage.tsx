import { useAuth } from '../auth/AuthContext'

export function ShellPage() {
  const { user } = useAuth()

  return (
    <>
      <h1>Welcome, {user?.displayName}</h1>
      <p>You're signed in and viewing the household budget shell.</p>
    </>
  )
}
