import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Icon } from './Icon'

interface Props {
  title: string
  eyebrow?: ReactNode
  backTo?: string
  action?: ReactNode
}

export function TopBar({ title, eyebrow, backTo, action }: Props) {
  return (
    <header className="top-bar">
      {backTo && (
        <Link className="icon-button" to={backTo} aria-label="Back">
          <Icon name="back" />
        </Link>
      )}
      <div className="top-bar-title">
        {eyebrow && <p className="live-eyebrow">{eyebrow}</p>}
        <h1>{title}</h1>
      </div>
      {action}
    </header>
  )
}
