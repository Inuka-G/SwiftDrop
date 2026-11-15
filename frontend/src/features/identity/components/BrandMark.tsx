import { TruckIcon } from './Icons'

export function BrandMark({ size = 'md' }: { size?: 'md' | 'lg' }) {
  return (
    <span className={`sd-brand-mark sd-brand-mark--${size}`} aria-hidden="true">
      <TruckIcon />
    </span>
  )
}
