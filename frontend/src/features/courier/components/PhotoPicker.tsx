import { useId, useRef, useState, type ChangeEvent } from 'react'
import { useObjectUrl } from '../../../hooks/useObjectUrl'
import { Icon } from './Icon'

const MAX_PHOTO_BYTES = 5 * 1024 * 1024
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp']

interface Props {
  title: string
  hint: string
  file: File | null
  onChange: (file: File | null) => void
}

/** Opens the phone camera (or file picker) and previews the photo. The server re-checks type and size. */
export function PhotoPicker({ title, hint, file, onChange }: Props) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const preview = useObjectUrl(file)
  const [error, setError] = useState<string | null>(null)

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const picked = event.target.files?.[0] ?? null
    event.target.value = ''   // lets the same file be chosen again after removing it
    if (!picked) return
    if (!ACCEPTED_TYPES.includes(picked.type)) return setError('Choose a JPEG, PNG or WebP photo.')
    if (picked.size > MAX_PHOTO_BYTES) return setError('The photo must be 5 MB or smaller.')
    setError(null)
    onChange(picked)
  }

  return (
    <section className="card form-card">
      <div className="form-card-head">
        <div>
          <h2>{title}</h2>
          <p className="muted">{hint}</p>
        </div>
        {file && (
          <span className="pill pill-success">
            <Icon name="camera" size={14} /> Attached
          </span>
        )}
      </div>
      <input
        ref={inputRef}
        id={inputId}
        className="visually-hidden"
        type="file"
        accept={ACCEPTED_TYPES.join(',')}
        capture="environment"
        onChange={handleChange}
      />
      {preview ? (
        <div className="photo-frame">
          <img src={preview} alt="Selected photo" />
          <div className="photo-frame-actions">
            <button type="button" className="chip-button" onClick={() => inputRef.current?.click()}>
              <Icon name="camera" size={16} /> Retake
            </button>
            <button type="button" className="chip-button" onClick={() => onChange(null)}>
              Remove
            </button>
          </div>
        </div>
      ) : (
        <label htmlFor={inputId} className="photo-drop">
          <Icon name="camera" size={28} />
          <span>Take or upload a photo</span>
          <small>JPEG, PNG or WebP · up to 5 MB</small>
        </label>
      )}
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
    </section>
  )
}
