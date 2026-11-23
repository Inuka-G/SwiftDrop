import axios from 'axios'
import { useEffect, useMemo, useState } from 'react'
import { authService } from '../../../services/auth.service'
import { COURIER_SEARCH_DEBOUNCE_MS, USE_MOCKS } from '../../../lib/constants'
import { useCourierWorkload } from '../hooks/useCourierWorkload'
import { useDeleteCourier, useUpdateCourier } from '../hooks/useCourierMutations'
import { CourierToast, type CourierToastMessage } from '../components/CourierToast'
import { RegisterCourierDialog } from '../components/RegisterCourierDialog'
import { EditCourierDialog } from '../components/EditCourierDialog'
import type { CourierDto } from '../types/dispatch.types'
import { courierInitials, courierStats, filterAndSortCouriers, type CourierAvailabilityFilter } from '../utils/courierWorkload.utils'
import { loadBand, loadRatio, shortId, vehicleGlyph } from '../utils/dispatchBoard.utils'

const EMPTY_COURIERS: CourierDto[] = []

function WorkloadSkeleton() {
  return <>
    <div className="courier-stat-grid" aria-hidden="true">{Array.from({ length: 4 }, (_, index) => <div className="courier-stat-skeleton" key={index} />)}</div>
    <div className="courier-table-card"><div className="courier-table-scroll"><table className="courier-table"><tbody>{Array.from({ length: 10 }, (_, index) => <tr className="courier-skeleton-row" key={index}><td colSpan={7}><i /></td></tr>)}</tbody></table></div></div>
  </>
}

function WorkloadError({ unauthorized, retry }: { unauthorized: boolean; retry: () => void }) {
  return <div className="courier-table-state" role="alert"><strong>{unauthorized ? 'Your session has expired or you cannot access courier workload.' : 'We could not load the courier list.'}</strong><button type="button" onClick={() => { if (unauthorized) { authService.clearAccessToken(); window.location.assign('/admin') } else retry() }}>{unauthorized ? 'Sign in again' : 'Try again'}</button></div>
}

function EmptyFleet({ onRegister }: { onRegister: (opener: HTMLElement) => void }) {
  return <section className="courier-empty-state"><span aria-hidden="true">▰</span><h2>No couriers registered yet</h2><p>Start building your dispatch workforce. Add drivers, assign regional zones, and track active deliveries on the live workload board.</p><button type="button" className="dispatch-primary-button" onClick={(event) => onRegister(event.currentTarget)}>＋ Register courier</button></section>
}

export function CourierWorkloadPage() {
  const [dialog, setDialog] = useState<'register' | 'edit' | null>(null)
  const [selectedCourier, setSelectedCourier] = useState<CourierDto | null>(null)
  const [dialogOpener, setDialogOpener] = useState<HTMLElement | null>(null)
  const [toast, setToast] = useState<CourierToastMessage | null>(null)
  const [rawSearch, setRawSearch] = useState('')
  const [search, setSearch] = useState('')
  const [availability, setAvailability] = useState<CourierAvailabilityFilter>('all')
  const [mostActiveFirst, setMostActiveFirst] = useState(false)
  const query = useCourierWorkload(dialog !== null)
  const updateMutation = useUpdateCourier()
  const deleteMutation = useDeleteCourier()
  const couriers = query.data?.items ?? EMPTY_COURIERS
  const totalCount = query.data?.totalCount ?? 0
  const stats = useMemo(() => courierStats(couriers), [couriers])
  const filteredCouriers = useMemo(() => filterAndSortCouriers(couriers, search, availability, mostActiveFirst), [couriers, search, availability, mostActiveFirst])
  const maxWorkload = Math.max(1, ...couriers.map((courier) => courier.activeDeliveryCount))
  const filtered = Boolean(rawSearch.trim() || availability !== 'all')
  const unauthorized = axios.isAxiosError(query.error) && [401, 403].includes(query.error.response?.status ?? 0)
  const loading = query.isLoading
  const activeEditCourier = selectedCourier ? couriers.find((courier) => courier.courierId === selectedCourier.courierId) ?? selectedCourier : null

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(rawSearch), COURIER_SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(timer)
  }, [rawSearch])

  function openRegister(opener: HTMLElement) {
    setDialogOpener(opener)
    setSelectedCourier(null)
    setDialog('register')
  }

  function openEdit(courier: CourierDto, opener: HTMLElement | null) {
    setDialogOpener(opener)
    setSelectedCourier(courier)
    setDialog('edit')
  }

  function closeDialog() {
    setDialog(null)
    setSelectedCourier(null)
  }

  function clearFilters() {
    setRawSearch('')
    setSearch('')
    setAvailability('all')
  }

  async function toggleAvailability(courier: CourierDto, nextAvailable: boolean) {
    try {
      await updateMutation.mutateAsync({ courierId: courier.courierId, request: { isAvailable: nextAvailable } })
      setToast({ title: 'Courier status updated', message: `${courier.name} is ${nextAvailable ? 'available' : 'unavailable'} for dispatch.`, kind: 'success' })
    } catch (cause) {
      const status = axios.isAxiosError(cause) ? cause.response?.status : undefined
      if (status === 409) {
        await query.refetch()
        setToast({ title: 'Courier updated elsewhere', message: 'This courier was just updated by someone else. The list has been refreshed.', kind: 'error' })
      } else if (status === 401 || status === 403) {
        authService.clearAccessToken()
        window.location.assign('/admin')
      } else {
        setToast({ title: 'Update failed', message: "Couldn't update the courier. Try again.", kind: 'error' })
      }
    }
  }

  async function deleteRegisteredCourier(courier: CourierDto) {
    const confirmed = window.confirm(
      `Remove ${courier.name} from the dispatch courier list? Their SwiftDrop user account will remain. Couriers with active deliveries cannot be removed.`,
    )
    if (!confirmed) return

    try {
      await deleteMutation.mutateAsync(courier.courierId)
      setToast({ title: 'Courier removed', message: `${courier.name} was removed from the dispatch workload. Their account was kept.`, kind: 'success' })
    } catch (cause) {
      const status = axios.isAxiosError(cause) ? cause.response?.status : undefined
      if (status === 401 || status === 403) {
        authService.clearAccessToken()
        window.location.assign('/admin')
      } else if (status === 409) {
        await query.refetch()
        setToast({ title: 'Courier has active deliveries', message: 'Reassign or complete their active deliveries before removing this courier.', kind: 'error' })
      } else if (status === 404) {
        await query.refetch()
        setToast({ title: 'Courier already removed', message: 'The courier list has been refreshed.', kind: 'info' })
      } else {
        setToast({ title: 'Removal failed', message: "Couldn't remove this courier. Try again.", kind: 'error' })
      }
    }
  }

  async function refreshSelectedCourier(): Promise<CourierDto | null> {
    const result = await query.refetch()
    if (!selectedCourier) return null
    return result.data?.items.find((courier) => courier.courierId === selectedCourier.courierId) ?? null
  }

  if (loading) return <main className="courier-workload-page"><WorkloadSkeleton /></main>

  return <main className="courier-workload-page">
    <header className="courier-workload-header">
      <div><p className="courier-workload-eyebrow"><span aria-hidden="true">♧</span> Fleet capacity &amp; utilization</p><h1>Courier workload</h1></div>
      <button type="button" className="dispatch-primary-button courier-register-button" onClick={(event) => openRegister(event.currentTarget)}>＋ Register courier</button>
    </header>

    {USE_MOCKS && import.meta.env.DEV && <p className="dispatch-sample-tag">Sample data</p>}
    {query.isError ? <WorkloadError unauthorized={unauthorized} retry={() => void query.refetch()} /> : <>
      <section className="courier-stat-grid" aria-label="Courier workload summary">
        <article className="courier-stat-card"><span>Total active fleet</span><strong>{stats.total}</strong>{stats.availablePercent !== null && <small>{stats.availablePercent}% available</small>}</article>
        <article className="courier-stat-card"><span>Available now</span><strong className="is-green">{stats.available}</strong><small>Ready for assignment</small></article>
        <article className="courier-stat-card"><span>Peak unit workload</span><strong className="is-orange">{stats.peakWorkload}</strong><small>Active deliveries</small></article>
        <article className="courier-stat-card"><span>Default sort</span><strong className="courier-sort-value">↕</strong><small>{mostActiveFirst ? 'Most jobs first' : 'Fewest jobs first'}</small></article>
      </section>

      {couriers.length === 0 && totalCount === 0 && !filtered ? <EmptyFleet onRegister={openRegister} /> : <>
        <section className="courier-filter-bar" aria-label="Filter couriers">
          <label className="courier-search-field"><span aria-hidden="true">⌕</span><input value={rawSearch} onChange={(event) => setRawSearch(event.target.value)} placeholder="Search courier name or plate (e.g. WP CAD)…" aria-label="Search courier name or license plate" /></label>
          <label className="courier-availability-filter"><span className="courier-sr-only">Availability</span><select value={availability} onChange={(event) => setAvailability(event.target.value as CourierAvailabilityFilter)}><option value="all">All Availability</option><option value="available">Available</option><option value="unavailable">Unavailable</option></select></label>
          <span className="courier-count-label">Showing {filteredCouriers.length} courier{filteredCouriers.length === 1 ? '' : 's'}</span>
          <button type="button" className="courier-clear-link" disabled={!filtered} onClick={clearFilters}>Clear</button>
        </section>
        {totalCount > couriers.length && <p className="courier-page-limit">Showing the first {couriers.length} of {totalCount} couriers.</p>}
        {filtered && filteredCouriers.length === 0 ? <section className="courier-no-match"><span aria-hidden="true">⌕</span><h2>No matching couriers</h2><p>Adjust search parameters or selected filters to view results.</p><button type="button" className="dispatch-secondary-button" onClick={clearFilters}>Reset all filters</button></section> : <section className="courier-table-card" aria-label="Courier workload">
          <div className="courier-table-scroll"><table className="courier-table">
            <thead><tr><th scope="col">Courier</th><th scope="col">Zone</th><th scope="col">Vehicle</th><th scope="col">License plate</th><th scope="col">Availability</th><th scope="col" aria-sort={mostActiveFirst ? 'descending' : 'ascending'}><button type="button" className="courier-sort-heading" onClick={() => setMostActiveFirst((value) => !value)}>Active deliveries <span aria-hidden="true">{mostActiveFirst ? '↓' : '↑'}</span></button></th><th scope="col" className="courier-actions-heading">Actions</th></tr></thead>
            <tbody>{filteredCouriers.map((courier) => {
              const ratio = loadRatio(courier.activeDeliveryCount, maxWorkload)
              const pending = (updateMutation.isPending && updateMutation.variables?.courierId === courier.courierId)
                || (deleteMutation.isPending && deleteMutation.variables === courier.courierId)
              return <tr key={courier.courierId}>
                <td><span className="courier-person-cell"><span className="courier-avatar">{courierInitials(courier.name)}</span><span><strong title={courier.name}>{courier.name}</strong><small>ID #{shortId(courier.courierId)}</small></span></span></td>
                <td><span className="courier-zone-chip" title={courier.zoneId ?? undefined}>{courier.zoneId ? shortId(courier.zoneId) : 'No zone'}</span></td>
                <td><span className="courier-vehicle-cell"><b aria-hidden="true">{vehicleGlyph(courier.vehicleType)}</b>{courier.vehicleType || 'Not set'}</span></td>
                <td><span className="courier-plate-chip">{courier.licensePlate?.trim() || 'Not set'}</span></td>
                <td><span className={`courier-availability-badge${courier.isAvailable ? ' is-available' : ''}`}><i aria-hidden="true" />{courier.isAvailable ? 'Available' : 'Unavailable'}</span></td>
                <td><span className="courier-workload-cell"><strong>{courier.activeDeliveryCount}</strong><span className="courier-load-track"><i className={`is-${loadBand(ratio)}`} style={{ width: `${ratio}%` }} /></span></span></td>
                <td><span className="courier-row-actions"><button type="button" role="switch" aria-checked={courier.isAvailable} aria-label={`Available for dispatch, ${courier.name}`} title="Unavailable couriers cannot receive new parcels" disabled={pending} className={`courier-toggle${courier.isAvailable ? ' is-on' : ''}${pending ? ' is-pending' : ''}`} onClick={() => void toggleAvailability(courier, !courier.isAvailable)}><i /></button><button type="button" className="courier-edit-button" aria-label={`Edit ${courier.name}`} title={`Edit ${courier.name}`} onClick={(event) => openEdit(courier, event.currentTarget)}>✎</button><button type="button" className="courier-delete-button" aria-label={`Remove ${courier.name}`} title="Remove from dispatch" disabled={pending} onClick={() => void deleteRegisteredCourier(courier)}><svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M4 7h16M10 11v6m4-6v6M6 7l1 13h10l1-13M9 7V4h6v3" /></svg></button></span></td>
              </tr>
            })}</tbody>
          </table></div>
        </section>}
      </>}
    </>}

    {dialog === 'register' && <RegisterCourierDialog couriers={couriers} opener={dialogOpener} onClose={closeDialog} onRegistered={(courier) => setToast({ title: 'Courier registered', message: `${courier.name} is now on the workload board.`, kind: 'success' })} onEditExisting={(courier, opener) => openEdit(courier, opener)} />}
    {dialog === 'edit' && activeEditCourier && <EditCourierDialog key={`${activeEditCourier.courierId}-${query.dataUpdatedAt}`} courier={activeEditCourier} opener={dialogOpener} onClose={closeDialog} onUpdated={(courier) => setToast({ title: 'Courier updated', message: `${courier.name}'s profile has been updated.`, kind: 'success' })} onRefresh={refreshSelectedCourier} />}
    {toast && <CourierToast data={toast} onClose={() => setToast(null)} />}
  </main>
}
