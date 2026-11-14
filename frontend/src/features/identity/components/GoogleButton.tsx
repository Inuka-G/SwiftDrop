import { GOOGLE_LOGIN_URL } from '../../../lib/constants'

/**
 * Starts "Login with Google". It is a normal link (not a fetch), because the browser itself
 * must travel to Google and back. To use Google's official button artwork, see
 * https://developers.google.com/identity/branding-guidelines
 */
export function GoogleButton({ label }: { label: string }) {
  return (
    <a className="sd-btn sd-btn--google" href={GOOGLE_LOGIN_URL}>
      <span className="sd-google-mark" aria-hidden="true">G</span>
      {label}
    </a>
  )
}
