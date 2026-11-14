import { useId, useState, type InputHTMLAttributes, type ReactNode } from 'react'
import { EyeIcon, EyeOffIcon } from './Icons'

interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  icon?: ReactNode
  /** Extra content on the right of the label, such as a link. */
  labelAside?: ReactNode
  hint?: ReactNode
  error?: string
}

/** A labelled input with an optional icon. Password fields get a show/hide button. */
export function FormField({ label, icon, labelAside, hint, error, type = 'text', ...inputProps }: FormFieldProps) {
  const id = useId()
  const [visible, setVisible] = useState(false)
  const isPassword = type === 'password'
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined

  return (
    <div className="sd-field">
      <div className="sd-field__label-row">
        <label htmlFor={id}>{label}</label>
        {labelAside}
      </div>
      <div className={`sd-input${error ? ' sd-input--invalid' : ''}`}>
        {icon && <span className="sd-input__icon">{icon}</span>}
        <input id={id} type={isPassword && visible ? 'text' : type} aria-invalid={!!error}
          aria-describedby={describedBy} {...inputProps} />
        {isPassword && (
          <button type="button" className="sd-input__toggle" onClick={() => setVisible((v) => !v)}
            aria-label={visible ? 'Hide password' : 'Show password'}>
            {visible ? <EyeOffIcon /> : <EyeIcon />}
          </button>
        )}
      </div>
      {error ? (
        <p id={`${id}-error`} className="sd-field__error">{error}</p>
      ) : (
        hint && <p id={`${id}-hint`} className="sd-field__hint">{hint}</p>
      )}
    </div>
  )
}
