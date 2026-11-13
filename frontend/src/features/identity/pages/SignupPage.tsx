import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { homePathForRole } from '../../../lib/constants'
import { BrandMark } from '../components/BrandMark'
import { FormAlert } from '../components/FormAlert'
import { FormField } from '../components/FormField'
import { GoogleButton } from '../components/GoogleButton'
import { ArrowRightIcon, LockIcon, MailIcon, UserIcon } from '../components/Icons'
import { useAuth } from '../useAuth'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
// Sri Lankan mobile numbers: 9 digits after +94, starting with 7.
const LK_MOBILE_PATTERN = /^7\d{8}$/

interface FormValues {
  fullName: string
  email: string
  phone: string
  password: string
  confirmPassword: string
  agreed: boolean
}

type FormErrors = Partial<Record<keyof FormValues, string>>

/** Turns "077 123 4567" or "77 123 4567" into "771234567". */
function normalizePhone(phone: string): string {
  return phone.replace(/\D/g, '').replace(/^0/, '')
}

function validate(values: FormValues): FormErrors {
  const errors: FormErrors = {}
  if (!values.fullName.trim()) errors.fullName = 'Enter your full name.'
  if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Enter a valid email address.'
  if (values.phone.trim() && !LK_MOBILE_PATTERN.test(normalizePhone(values.phone)))
    errors.phone = 'Enter a mobile number like 77 123 4567.'
  if (values.password.length < 8) errors.password = 'Password must be at least 8 characters long.'
  if (values.confirmPassword !== values.password) errors.confirmPassword = 'Passwords do not match.'
  if (!values.agreed) errors.agreed = 'You must agree to the Terms of Service and Privacy Policy.'
  return errors
}

export function SignupPage() {
  const { user, signup } = useAuth()
  const navigate = useNavigate()
  const [values, setValues] = useState<FormValues>({
    fullName: '', email: '', phone: '', password: '', confirmPassword: '', agreed: false,
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={homePathForRole(user.role)} replace />

  const set = <K extends keyof FormValues>(key: K, value: FormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
    if (errors[key]) setErrors((e) => ({ ...e, [key]: undefined }))
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setServerError(null)
    const found = validate(values)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    try {
      const phone = normalizePhone(values.phone)
      const created = await signup({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        phoneNumber: phone ? `+94${phone}` : undefined,
      })
      navigate(homePathForRole(created.role), { replace: true })
    } catch (err) {
      setServerError(getErrorMessage(err))
      setSubmitting(false)
    }
  }

  return (
    <div className="sd-auth-page sd-auth-page--column">
      <Link to="/" aria-label="SwiftDrop home"><BrandMark size="lg" /></Link>
      <h1 className="sd-title sd-center">Create your account</h1>
      <p className="sd-muted sd-center">Get started with SwiftDrop courier and tracking</p>

      <div className="sd-card sd-signup-card">
        <GoogleButton label="Sign up with Google" />
        <div className="sd-divider sd-divider--caps"><span>Or continue with email</span></div>

        <form onSubmit={handleSubmit} noValidate>
          <FormAlert message={serverError} />
          <FormField label="Full Name" icon={<UserIcon />} placeholder="e.g. Dinesh Weerasinghe" autoComplete="name"
            value={values.fullName} onChange={(e) => set('fullName', e.target.value)} error={errors.fullName} />
          <FormField label="Email Address" type="email" icon={<MailIcon />} placeholder="name@example.com"
            autoComplete="email" value={values.email} onChange={(e) => set('email', e.target.value)} error={errors.email} />

          <div className="sd-field">
            <div className="sd-field__label-row">
              <label htmlFor="signup-phone">Phone Number <span className="sd-optional">(optional)</span></label>
            </div>
            <div className="sd-phone">
              <span className="sd-phone__country"><span className="sd-phone__flag">LK</span> +94</span>
              <div className={`sd-input${errors.phone ? ' sd-input--invalid' : ''}`}>
                <input id="signup-phone" type="tel" inputMode="tel" className="sd-mono" placeholder="7X XXX XXXX"
                  autoComplete="tel-national" value={values.phone} onChange={(e) => set('phone', e.target.value)}
                  aria-invalid={!!errors.phone} aria-describedby={errors.phone ? 'signup-phone-error' : undefined} />
              </div>
            </div>
            {errors.phone && <p id="signup-phone-error" className="sd-field__error">{errors.phone}</p>}
          </div>

          <FormField label="Password" type="password" icon={<LockIcon />} placeholder="Create password"
            autoComplete="new-password" value={values.password} onChange={(e) => set('password', e.target.value)}
            hint="Must be at least 8 characters long" error={errors.password} />
          <FormField label="Confirm Password" type="password" icon={<LockIcon />} placeholder="Repeat your password"
            autoComplete="new-password" value={values.confirmPassword}
            onChange={(e) => set('confirmPassword', e.target.value)} error={errors.confirmPassword} />

          <label className="sd-checkbox">
            <input type="checkbox" checked={values.agreed} onChange={(e) => set('agreed', e.target.checked)} />
            <span>I agree to the <strong className="sd-text-primary">Terms of Service</strong> and{' '}
              <strong className="sd-text-primary">Privacy Policy</strong>.</span>
          </label>
          {errors.agreed && <p className="sd-field__error">{errors.agreed}</p>}

          <button type="submit" className="sd-btn sd-btn--primary sd-btn--block" disabled={submitting}>
            {submitting ? 'Creating account…' : <>Create Account <ArrowRightIcon /></>}
          </button>
        </form>

        <p className="sd-switch">Already have an account? <Link to="/login">Sign In</Link></p>
      </div>
    </div>
  )
}
