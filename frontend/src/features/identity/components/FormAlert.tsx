import { AlertIcon } from './Icons'

export function FormAlert({ message }: { message: string | null }) {
  if (!message) return null
  return (
    <div className="sd-alert" role="alert">
      <AlertIcon />
      <span>{message}</span>
    </div>
  )
}
