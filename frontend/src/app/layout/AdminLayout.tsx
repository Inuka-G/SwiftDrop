import { useState } from 'react'
import type { FormEvent } from 'react'
import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { Outlet } from 'react-router-dom'
import { AdminNav } from './AdminNav'
import { authService } from '../../services/auth.service'
import { storageService } from '../../services/storage.service'
import './AdminLayout.css'

export function AdminLayout() {
  const [user, setUser] = useState(storageService.getUser())
  const [loginError, setLoginError] = useState('')
  const [loggingIn, setLoggingIn] = useState(false)
  const queryClient = useQueryClient()
  const fetching = useIsFetching({ queryKey: ['dispatch'] }) + useIsFetching({ queryKey: ['reporting'] })
  const queryStates = queryClient.getQueryCache().findAll({ predicate: (query) => ['dispatch', 'reporting'].includes(String(query.queryKey[0])) })
  const hasConnectionIssue = queryStates.some((query) => query.state.status === 'error')
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoginError('')
    setLoggingIn(true)
    const form = new FormData(event.currentTarget)
    try {
      await authService.login(String(form.get('username')), String(form.get('password')))
      const signedInUser = storageService.getUser()
      if (!signedInUser || !['ADMIN', 'DISPATCHER'].includes(signedInUser.role.toUpperCase())) {
        authService.clearAccessToken()
        throw new Error('This dashboard requires a dispatcher or admin account.')
      }
      setUser(signedInUser)
    } catch (error) {
      setLoginError(error instanceof Error ? error.message : 'Sign in failed. Check the API connection and credentials.')
    } finally {
      setLoggingIn(false)
    }
  }
  return (
    <div className="admin-shell">
      <AdminNav hasConnectionIssue={Boolean(user) && hasConnectionIssue && fetching === 0} user={user} onLogout={() => setUser(null)} />
      {user ? <Outlet /> : <main style={{ maxWidth: 420, margin: '4rem auto', padding: '2rem', background: 'white', borderRadius: 16 }}>
        <h1>Dispatcher sign in</h1>
        <p>Use your SwiftDrop dispatcher or admin account.</p>
        <form onSubmit={login} style={{ display: 'grid', gap: 12 }}>
          <label>Username<input name="username" autoComplete="username" required /></label>
          <label>Password<input name="password" type="password" autoComplete="current-password" required /></label>
          {loginError && <p role="alert" style={{ color: '#b42318' }}>{loginError}</p>}
          <button type="submit" disabled={loggingIn}>{loggingIn ? 'Signing in…' : 'Sign in'}</button>
        </form>
        <p>Development demo: username <code>admin</code>, password <code>admin</code>.</p>
      </main>}
    </div>
  )
}
