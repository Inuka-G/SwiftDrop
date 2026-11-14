import { useEffect, useRef } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { homePathForRole } from '../../../lib/constants'
import { BrandMark } from '../components/BrandMark'
import { useAuth } from '../useAuth'

/**
 * The backend sends the browser here after Google login, with either ?code=... or ?error=...
 * The code works only once, so it is exchanged exactly once, even though React's
 * development mode runs effects twice.
 */
export function GoogleCallbackPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const { completeGoogleLogin } = useAuth()
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true

    const code = params.get('code')
    const backendError = params.get('error')
    const fail = (message: string) => navigate('/login', { replace: true, state: { error: message } })

    if (!code) {
      fail(backendError ?? 'Google sign-in failed. Please try again.')
      return
    }

    completeGoogleLogin(code)
      .then((user) => navigate(homePathForRole(user.role), { replace: true }))
      .catch((err) => fail(getErrorMessage(err, 'Google sign-in failed. Please try again.')))
  }, [params, navigate, completeGoogleLogin])

  return (
    <div className="sd-auth-page sd-auth-page--column" aria-live="polite">
      <BrandMark size="lg" />
      <p className="sd-muted">Signing you in with Google…</p>
    </div>
  )
}
