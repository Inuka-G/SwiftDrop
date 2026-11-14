import axios from 'axios'
import { useEffect, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { authService } from '../../../services/auth.service'
import { useUnassignedParcels } from '../hooks/useUnassignedParcels'
import { useCouriers } from '../hooks/useCouriers'
import { AssignCourierDialog } from '../components/AssignCourierDialog'
import { AssignmentToast } from '../components/AssignmentToast'
import type { AssignmentToastData, DispatchFilters, UnassignedParcelDto } from '../types/dispatch.types'
import { DISPATCH_PAGE_SIZES, USE_MOCKS } from '../../../lib/constants'
import { formatCreatedAt, formatWaitingSince, getPageItems, getShowingRange, isDateRangeValid, isOverdue, lastValidPage, parcelZoneLabel } from '../utils/dispatchBoard.utils'

function TableSkeleton() {
  return <>
    {Array.from({ length: 10 }, (_, index) => <tr className="dispatch-skeleton-row" key={index} aria-hidden="true"><td colSpan={6}><i /></td></tr>)}
    <tr className="dispatch-skeleton-footer" aria-hidden="true"><td colSpan={6}><i /></td></tr>
  </>
}

function ErrorRows({ unauthorized, retry }: { unauthorized: boolean; retry: () => void }) {
  return <tr><td colSpan={6}><div className="dispatch-table-state" role="alert"><strong>{unauthorized ? 'Your session has expired or you cannot access Dispatch.' : 'We could not load the unassigned parcels.'}</strong>{unauthorized ? <button type="button" onClick={() => { authService.clearAccessToken(); window.location.assign('/admin') }}>Sign in again</button> : <button type="button" onClick={retry}>Try again</button>}</div></td></tr>
}

export function DispatchBoardPage() {
  const [filters, setFilters] = useState<DispatchFilters>({ from: '', to: '' })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<number>(10)
  const [selectedParcel, setSelectedParcel] = useState<UnassignedParcelDto | null>(null)
  const [dialogOpener, setDialogOpener] = useState<HTMLElement | null>(null)
  const [toast, setToast] = useState<AssignmentToastData | null>(null)
  const [clockTime, setClockTime] = useState(0)
  const queryClient = useQueryClient()
  const query = useUnassignedParcels(filters, page, pageSize)
  const couriers = useCouriers(false)
  const isValid = isDateRangeValid(filters)
  const data = query.data
  const now = clockTime || query.dataUpdatedAt
  const total = isValid ? data?.totalCount ?? 0 : 0
  const pageCount = lastValidPage(total, pageSize)
  const range = useMemo(() => getShowingRange(page, pageSize, total), [page, pageSize, total])
  const unauthorized = axios.isAxiosError(query.error) && [401, 403].includes(query.error.response?.status ?? 0)

  useEffect(() => {
    const timer = window.setInterval(() => setClockTime(Date.now()), 60_000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    const validPage = data ? lastValidPage(data.totalCount, pageSize) : page
    if (page > validPage) {
      const timer = window.setTimeout(() => setPage(validPage), 0)
      return () => window.clearTimeout(timer)
    }
  }, [data, page, pageSize])

  function updateFilter(name: keyof DispatchFilters, value: string) {
    setFilters((current) => ({ ...current, [name]: value }))
    setPage(1)
  }

  function clearFilters() {
    setFilters({ from: '', to: '' })
    setPage(1)
  }

  async function refreshBothLists() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['dispatch', 'unassigned'] }),
      queryClient.invalidateQueries({ queryKey: ['dispatch', 'couriers'] }),
      queryClient.invalidateQueries({ queryKey: ['dispatch', 'summary'] }),
    ])
    await query.refetch()
    await couriers.refetch()
  }

  function openAssignment(parcel: UnassignedParcelDto, opener: HTMLElement) {
    setDialogOpener(opener)
    setSelectedParcel(parcel)
  }

  const rows = data?.items ?? []
  const filtered = Boolean(filters.from || filters.to)
  const showLoading = isValid && query.isLoading

  return <main className="dispatch-board-page">
    <header className="dispatch-board-header">
      <div className="dispatch-board-title-group"><span className="dispatch-title-accent" aria-hidden="true" /><h1>Dispatch Board</h1><span className="dispatch-unassigned-badge"><i aria-hidden="true" />{total} unassigned</span><span className="dispatch-active-queue">/ Active Queue</span></div>
      <button type="button" className={`dispatch-refresh-button${query.isFetching ? ' is-spinning' : ''}`} aria-label="Refresh unassigned parcels" title="Refresh queue" disabled={query.isFetching || !isValid} onClick={() => void query.refetch()}><span aria-hidden="true">↻</span></button>
    </header>

    {USE_MOCKS && import.meta.env.DEV && <p className="dispatch-sample-tag">Sample data</p>}
    <section className="dispatch-filter-card" aria-label="Filter unassigned parcels">
      <label className="dispatch-date-filter"><span>Created from</span><input type="date" value={filters.from} onChange={(event) => updateFilter('from', event.target.value)} /></label>
      <label className="dispatch-date-filter"><span>Created to</span><input type="date" value={filters.to} onChange={(event) => updateFilter('to', event.target.value)} /></label>
      <button type="button" className="dispatch-clear-button" onClick={clearFilters} disabled={!filtered}>× <span>Clear filters</span></button>
      {!isValid && <p className="dispatch-filter-error" role="alert">The start date must be on or before the end date</p>}
    </section>

    <section className="dispatch-table-card" aria-label="Unassigned parcels">
      <div className="dispatch-table-scroll"><table className="dispatch-table"><thead><tr><th scope="col">Tracking number</th><th scope="col">Zone</th><th scope="col">Size</th><th scope="col">Weight (kg)</th><th scope="col">Waiting since</th><th scope="col" className="dispatch-action-heading">Action</th></tr></thead>
        <tbody>
          {showLoading && <TableSkeleton />}
          {!showLoading && query.isError && <ErrorRows unauthorized={unauthorized} retry={() => void query.refetch()} />}
          {!showLoading && isValid && !query.isError && rows.length === 0 && !filtered && <tr><td colSpan={6}><div className="dispatch-empty-state"><span aria-hidden="true">✓</span><strong>No unassigned parcels. Everything is assigned.</strong><small>All parcels have been assigned to couriers.</small><button type="button" className="dispatch-secondary-button" onClick={() => void query.refetch()}>↻ Refresh</button></div></td></tr>}
          {!showLoading && isValid && !query.isError && rows.length === 0 && filtered && <tr><td colSpan={6}><div className="dispatch-empty-state"><strong>No parcels match your filters</strong><button type="button" className="dispatch-secondary-button" onClick={clearFilters}>Clear filters</button></div></td></tr>}
          {!showLoading && isValid && !query.isError && rows.map((parcel) => {
            const overdue = isOverdue(parcel.createdAt, now)
            return <tr key={parcel.parcelId}>
              <td><span className={`dispatch-tracking-cell${overdue ? ' is-overdue' : ''}`}><i aria-hidden="true" /><strong>{parcel.trackingNumber}</strong></span></td>
              <td><span className="dispatch-zone-chip" title={parcel.zoneId}>{parcelZoneLabel(parcel)}</span></td>
              <td><span className="dispatch-size-tag">{parcel.sizeCategory || '—'}</span></td>
              <td className="dispatch-weight-cell">{parcel.weightKg == null ? '—' : `${parcel.weightKg} kg`}</td>
              <td><span className={`dispatch-wait-cell${overdue ? ' is-overdue' : ''}`}><strong>{formatWaitingSince(parcel.createdAt, now)}</strong><small>{formatCreatedAt(parcel.createdAt)}</small></span></td>
              <td className="dispatch-action-cell"><button type="button" className="dispatch-assign-button" aria-label={`Assign courier for ${parcel.trackingNumber}`} onClick={(event) => openAssignment(parcel, event.currentTarget)}>Assign</button></td>
            </tr>
          })}
        </tbody>
      </table></div>
      {!showLoading && isValid && !query.isError && !(rows.length === 0 && !filtered) && !(rows.length === 0 && filtered) && <footer className="dispatch-table-footer">
        <label className="dispatch-page-size"><span>Show</span><select aria-label="Rows per page" value={pageSize} onChange={(event) => { setPageSize(Number(event.target.value)); setPage(1) }}>{DISPATCH_PAGE_SIZES.map((size) => <option key={size} value={size}>{size}</option>)}</select></label>
        <span className="dispatch-range-label">{range}</span>
        <nav className="dispatch-pagination" aria-label="Parcel pages"><button type="button" className="dispatch-page-control" aria-label="Previous page" disabled={page <= 1} onClick={() => setPage((current) => Math.max(1, current - 1))}>‹ Previous</button>{getPageItems(page, pageCount).map((item, index) => item === 'ellipsis' ? <span className="dispatch-page-ellipsis" key={`ellipsis-${index}`}>…</span> : <button type="button" className={`dispatch-page-number${item === page ? ' is-current' : ''}`} key={item} aria-current={item === page ? 'page' : undefined} onClick={() => setPage(item)}>{item}</button>)}<button type="button" className="dispatch-page-control" aria-label="Next page" disabled={page >= pageCount} onClick={() => setPage((current) => Math.min(pageCount, current + 1))}>Next ›</button></nav>
      </footer>}
    </section>

    {selectedParcel && <AssignCourierDialog parcel={selectedParcel} opener={dialogOpener} onClose={() => setSelectedParcel(null)} onAssigned={setToast} onRefreshList={refreshBothLists} />}
    {toast && <AssignmentToast data={toast} onClose={() => setToast(null)} />}
  </main>
}
