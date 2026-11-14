import axios from 'axios'
import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useAssignParcel } from '../hooks/useAssignParcel'
import { useCouriers } from '../hooks/useCouriers'
import type { AssignmentToastData, CourierDto, UnassignedParcelDto } from '../types/dispatch.types'
import { activeDeliveryLabel, assignmentErrorKind, courierName, filterCouriers, isZoneMatch, loadBand, loadRatio, shortId, sortCouriers, vehicleGlyph } from '../utils/dispatchBoard.utils'
import { USE_MOCKS } from '../../../lib/constants'

type DialogError = ReturnType<typeof assignmentErrorKind> | 'courier-list'
const EMPTY_COURIERS: CourierDto[] = []

export function AssignCourierDialog({ parcel, opener, onClose, onAssigned, onRefreshList }: {
  parcel: UnassignedParcelDto
  opener: HTMLElement | null
  onClose: () => void
  onAssigned: (data: AssignmentToastData) => void
  onRefreshList: () => Promise<void>
}) {
  const couriersQuery = useCouriers(true)
  const mutation = useAssignParcel()
  const [search, setSearch] = useState('')
  const [mostActiveFirst, setMostActiveFirst] = useState(false)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<DialogError | null>(null)
  const dialogRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const couriers = couriersQuery.data ?? EMPTY_COURIERS
  const visibleCouriers = useMemo(() => sortCouriers(filterCouriers(couriers, search), mostActiveFirst), [couriers, search, mostActiveFirst])
  const maxActive = Math.max(1, ...couriers.map((courier) => courier.activeDeliveryCount))
  const selected = couriers.find((courier) => courier.courierId === selectedId) ?? null

  useEffect(() => {
    const root = document.getElementById('root')
    if (root) root.inert = true
    searchRef.current?.focus()
    return () => {
      if (root) root.inert = false
      window.setTimeout(() => opener?.focus(), 0)
    }
  }, [opener])

  function handleKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape' && !mutation.isPending) {
      event.preventDefault()
      onClose()
      return
    }
    if (event.key === 'Tab') {
      const focusable = dialogRef.current?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), [tabindex="0"]')
      if (!focusable?.length) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault(); last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault(); first.focus()
      }
    }
  }

  function moveSelection(event: React.KeyboardEvent<HTMLDivElement>) {
    if (!['ArrowDown', 'ArrowUp'].includes(event.key)) return
    const available = visibleCouriers.filter((courier) => courier.isAvailable)
    if (!available.length) return
    event.preventDefault()
    const current = available.findIndex((courier) => courier.courierId === selectedId)
    const direction = event.key === 'ArrowDown' ? 1 : -1
    const next = current < 0 ? 0 : (current + direction + available.length) % available.length
    const courier = available[next]
    setSelectedId(courier.courierId)
    dialogRef.current?.querySelector<HTMLElement>(`[data-courier-id="${courier.courierId}"]`)?.focus()
  }

  async function submit() {
    if (!selected || mutation.isPending) return
    setError(null)
    try {
      await mutation.mutateAsync({ parcelId: parcel.parcelId, courierId: selected.courierId })
      onAssigned({ parcel, courier: selected })
      onClose()
    } catch (err) {
      const status = axios.isAxiosError(err) ? err.response?.status : (err as { response?: { status?: number } })?.response?.status
      const kind = assignmentErrorKind(status)
      setError(kind)
      if (kind === 'ineligible') void couriersQuery.refetch()
    }
  }

  async function refreshAndClose() {
    await Promise.all([onRefreshList(), couriersQuery.refetch().then(() => undefined)])
    onClose()
  }

  const content = <div className="dispatch-dialog-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget && !mutation.isPending) onClose() }}>
    <section className="dispatch-dialog" role="dialog" aria-modal="true" aria-labelledby="assign-dialog-title" ref={dialogRef} onKeyDown={handleKeyDown}>
      <div className="dispatch-dialog-accent" />
      <header className="dispatch-dialog-header">
        <span className="dispatch-dialog-icon" aria-hidden="true">↗</span>
        <span className="dispatch-dialog-heading"><strong id="assign-dialog-title">Assign courier</strong><small>Select an available courier for this parcel</small></span>
        <button type="button" className="dispatch-icon-button" aria-label="Close assign courier dialog" disabled={mutation.isPending} onClick={onClose}>×</button>
      </header>
      {USE_MOCKS && import.meta.env.DEV && <p className="dispatch-sample-tag">Sample data</p>}

      <div className="dispatch-parcel-strip" aria-label="Selected parcel">
        <span><small>Tracking number</small><strong>{parcel.trackingNumber}</strong></span>
        <span><small>Zone</small><strong>⌖ {shortId(parcel.zoneId)}</strong></span>
        <span><small>Size</small><strong>{parcel.sizeCategory || '—'}</strong></span>
        <span><small>Weight</small><strong>{parcel.weightKg == null ? '—' : `${parcel.weightKg} kg`}</strong></span>
      </div>

      {error === 'conflict' && <div className="dispatch-dialog-alert" role="alert"><strong>Order Lock Conflict:</strong> This parcel was just assigned by someone else. Refresh the list.<button type="button" onClick={() => void refreshAndClose()}>Refresh List</button></div>}
      {error === 'not-found' && <div className="dispatch-dialog-alert" role="alert">This parcel no longer exists.<button type="button" onClick={() => void refreshAndClose()}>Refresh List</button></div>}
      {error === 'ineligible' && <div className="dispatch-dialog-alert" role="alert">This courier can't take this parcel. Choose another courier.</div>}
      {error === 'generic' && <div className="dispatch-dialog-alert" role="alert">The assignment could not be completed. Try again.<button type="button" onClick={() => setError(null)}>Try again</button></div>}
      {error === 'courier-list' && <div className="dispatch-dialog-alert" role="alert">Couldn't load couriers.<button type="button" onClick={() => { setError(null); void couriersQuery.refetch() }}>Try again</button></div>}

      <div className="dispatch-courier-toolbar">
        <label className="dispatch-search"><span aria-hidden="true">⌕</span><input ref={searchRef} value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search courier by name or zone…" aria-label="Search courier by name or zone" /></label>
        <button type="button" className="dispatch-sort" aria-pressed={mostActiveFirst} onClick={() => setMostActiveFirst((value) => !value)}>Sort: Active {mostActiveFirst ? '↓' : '↑'}</button>
      </div>

      <div className="dispatch-courier-list" role="radiogroup" aria-label="Available couriers" onKeyDown={moveSelection}>
        {couriersQuery.isLoading && Array.from({ length: 5 }, (_, index) => <div className="dispatch-courier-skeleton" key={index} />)}
        {couriersQuery.isError && <div className="dispatch-list-state">Couldn't load couriers.<button type="button" onClick={() => void couriersQuery.refetch()}>Try again</button></div>}
        {!couriersQuery.isLoading && !couriersQuery.isError && couriers.length === 0 && <div className="dispatch-list-state">No couriers are available yet.</div>}
        {!couriersQuery.isLoading && !couriersQuery.isError && couriers.length > 0 && visibleCouriers.length === 0 && <div className="dispatch-list-state">No couriers match your search.</div>}
        {!couriersQuery.isLoading && !couriersQuery.isError && visibleCouriers.map((courier, index) => {
          const ratio = loadRatio(courier.activeDeliveryCount, maxActive)
          const isSelected = selectedId === courier.courierId
          return <button type="button" role="radio" aria-checked={isSelected} aria-disabled={!courier.isAvailable} disabled={!courier.isAvailable || mutation.isPending} tabIndex={courier.isAvailable && (isSelected || (!selectedId && index === visibleCouriers.findIndex((item) => item.isAvailable))) ? 0 : -1} data-courier-id={courier.courierId} className={`dispatch-courier-option${isSelected ? ' is-selected' : ''}${courier.isAvailable ? '' : ' is-unavailable'}`} key={courier.courierId} title={courierName(courier)} onClick={() => setSelectedId(courier.courierId)}>
            <span className="dispatch-courier-select" aria-hidden="true">{courier.isAvailable ? (isSelected ? '✓' : '') : '⊘'}</span>
            <span className="dispatch-courier-name-zone"><strong>{courierName(courier)}</strong><span><i className={isZoneMatch(parcel, courier) ? 'dispatch-zone-match-dot' : ''} />{isZoneMatch(parcel, courier) && <em>Zone Match</em>}<small>{shortId(courier.zoneId)}</small></span></span>
            <span className="dispatch-courier-vehicle"><b aria-hidden="true">{vehicleGlyph(courier.vehicleType)}</b><small>{courier.vehicleType || 'Vehicle'}</small></span>
            <span className={`dispatch-availability${courier.isAvailable ? ' is-available' : ''}`}>{courier.isAvailable ? 'Available' : 'Unavailable'}</span>
            <span className="dispatch-load"><strong>{activeDeliveryLabel(courier.activeDeliveryCount)}</strong><span className="dispatch-load-track"><i className={`is-${loadBand(ratio)}`} style={{ width: `${ratio}%` }} /></span></span>
            <span className="dispatch-courier-chevron" aria-hidden="true">›</span>
          </button>
        })}
      </div>

      <footer className="dispatch-dialog-footer">
        <span className="dispatch-target"><small>Target:</small> <strong>{selected ? `${courierName(selected)} (${shortId(selected.zoneId)})` : 'Select a courier'}</strong></span>
        <div><button type="button" className="dispatch-secondary-button" disabled={mutation.isPending} onClick={onClose}>Cancel</button><button type="button" className="dispatch-primary-button" disabled={!selected || mutation.isPending} onClick={() => void submit()}>{mutation.isPending ? <><span className="dispatch-spinner" aria-hidden="true" />Assigning…</> : '✓ Assign courier'}</button></div>
      </footer>
    </section>
  </div>

  return createPortal(content, document.body)
}
