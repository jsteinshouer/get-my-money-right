import { useState, type FormEvent } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { status, login } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (status === 'authenticated') {
    const from = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={from} replace />
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
    } catch {
      setError('Incorrect email or password.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="container">
      <article style={{ maxWidth: '28rem', marginInline: 'auto' }}>
        <hgroup>
          <h1>Household Budget</h1>
          <p>Sign in to continue</p>
        </hgroup>
        <form onSubmit={handleSubmit}>
          <label htmlFor="email">
            Email
            <input
              id="email"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>
          <label htmlFor="password">
            Password
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>
          {error && (
            <p role="alert" style={{ color: 'var(--pico-form-element-invalid-active-border-color)' }}>
              {error}
            </p>
          )}
          <button type="submit" aria-busy={submitting} disabled={submitting}>
            Log in
          </button>
        </form>
      </article>
    </main>
  )
}
