import { Icon } from './Icon'

/** Call and turn-by-turn buttons for the stop the courier is heading to. */
export function ContactActions({ phone, address }: { phone: string; address: string }) {
  const directions = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(address)}`
  return (
    <div className="contact-actions">
      <a className="icon-button" href={`tel:${phone}`} aria-label={`Call ${phone}`}>
        <Icon name="phone" />
      </a>
      <a className="icon-button" href={directions} target="_blank" rel="noreferrer" aria-label="Open directions">
        <Icon name="navigate" />
      </a>
    </div>
  )
}
