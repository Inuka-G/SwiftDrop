
import { useEffect, useState } from 'react'
import type { FormEvent, CSSProperties } from 'react'
import { parcelsApi } from '../api/parcels.api'
import type {
  CreateParcelRequest,
  ParcelCreatedResponse,
  Zone,
} from '../types/parcel.types'

const initialForm: CreateParcelRequest = {
  zoneId: '',
  pickupAddress: '',
  pickupContactName: '',
  pickupContactPhone: '',
  dropoffAddress: '',
  recipientName: '',
  recipientPhone: '',
  weightKg: 1,
  sizeCategory: 'Small',
  paymentMethod: 'COD',
}

const parcelSizes = [
  { name: 'Small', description: 'Envelope or small parcel', icon: '▱' },
  { name: 'Medium', description: 'Standard-sized box', icon: '▢' },
  { name: 'Large', description: 'Large or bulky parcel', icon: '□' },
]

export function CreateBookingPage() {
  const [form, setForm] = useState<CreateParcelRequest>(initialForm)
  const [zones, setZones] = useState<Zone[]>([])
  const [zonesLoading, setZonesLoading] = useState(true)
  const [zonesError, setZonesError] = useState<string | null>(null)

  const [estimatedFee, setEstimatedFee] = useState<number | null>(null)
  const [feeLoading, setFeeLoading] = useState(false)
  const [feeError, setFeeError] = useState<string | null>(null)

  const [loading, setLoading] = useState(false)
  const [successResult, setSuccessResult] =
    useState<ParcelCreatedResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Load active delivery zones
  useEffect(() => {
    let cancelled = false

    async function loadZones() {
      try {
        const result = await parcelsApi.getZones()

        if (!cancelled) {
          setZones(result)

          if (result.length > 0) {
            setForm((previous) => ({
              ...previous,
              zoneId: previous.zoneId || result[0].id,
            }))
          }
        }
      } catch {
        if (!cancelled) {
          setZonesError('Unable to load delivery zones. Please try again.')
        }
      } finally {
        if (!cancelled) {
          setZonesLoading(false)
        }
      }
    }

    loadZones()

    return () => {
      cancelled = true
    }
  }, [])

  // Get the actual Standard delivery fee from the backend
  useEffect(() => {
    if (!form.zoneId || form.weightKg < 0.1 || form.weightKg > 50) {
      setEstimatedFee(null)
      setFeeError(null)
      setFeeLoading(false)
      return
    }

    let cancelled = false

    async function loadEstimate() {
      setFeeLoading(true)
      setFeeError(null)
      setEstimatedFee(null)

      try {
        const result = await parcelsApi.estimateFee(
          form.zoneId,
          form.weightKg,
        )

        if (!cancelled) {
          setEstimatedFee(result.total)
        }
      } catch {
        if (!cancelled) {
          setFeeError('Unable to calculate the fee. Please try again.')
        }
      } finally {
        if (!cancelled) {
          setFeeLoading(false)
        }
      }
    }

    loadEstimate()

    return () => {
      cancelled = true
    }
  }, [form.zoneId, form.weightKg])

  function updateField<K extends keyof CreateParcelRequest>(
    field: K,
    value: CreateParcelRequest[K],
  ) {
    setForm((previous) => ({
      ...previous,
      [field]: value,
    }))
  }

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const result = await parcelsApi.createParcel(form)
      setSuccessResult(result)
    } catch (err: unknown) {
      const message =
        typeof err === 'object' && err !== null && 'response' in err
          ? (err as {
              response?: {
                data?: { detail?: string; message?: string }
              }
            }).response?.data?.detail ??
            (err as {
              response?: {
                data?: { message?: string }
              }
            }).response?.data?.message
          : null

      setError(message || 'Failed to book parcel. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  async function handleDownloadReceipt() {
    if (!successResult) return

    try {
      await parcelsApi.downloadReceipt(successResult.parcelId)
    } catch {
      setError('Unable to download the receipt. Please try again.')
    }
  }

  const selectedZone = zones.find((zone) => zone.id === form.zoneId)

  const bookingDisabled =
    loading ||
    zonesLoading ||
    zones.length === 0 ||
    !form.zoneId ||
    feeLoading ||
    estimatedFee === null

  // Booking success screen
  if (successResult) {
    return (
      <div style={styles.successPage}>
        <div style={styles.successCard}>
          <div style={styles.successIcon}>✓</div>
          <span style={styles.successBadge}>BOOKING CONFIRMED</span>

          <h1 style={styles.successTitle}>
            Your delivery is booked!
          </h1>

          <p style={styles.description}>
            Your parcel booking has been successfully created.
            Keep your tracking number to follow its progress.
          </p>

          <div style={styles.trackingBox}>
            <span style={styles.smallLabel}>YOUR TRACKING NUMBER</span>
            <h2 style={styles.trackingNumber}>
              {successResult.trackingNumber}
            </h2>
          </div>

          <div style={styles.confirmedFee}>
            <span>Confirmed delivery fee</span>
            <strong>
              LKR {successResult.estimatedFee.toFixed(2)}
            </strong>
          </div>

          {error && <div style={styles.errorBox}>{error}</div>}

          <div style={styles.actions}>
            <button
              type="button"
              onClick={handleDownloadReceipt}
              style={styles.primaryButton}
            >
              Download Receipt PDF
            </button>

            <button
              type="button"
              onClick={() => {
                setSuccessResult(null)
                setForm({ ...initialForm, zoneId: zones[0]?.id ?? '' })
                setError(null)
              }}
              style={styles.secondaryButton}
            >
              Book Another Parcel
            </button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div style={styles.page}>
      {/* Page heading */}
      <div style={styles.pageHeading}>
        <div>
          <span style={styles.eyebrow}>CUSTOMER PORTAL / SEND PARCEL</span>
          <h1 style={styles.pageTitle}>Create a New Delivery</h1>
          <p style={styles.description}>
            Enter your parcel details to arrange a door-to-door delivery.
          </p>
        </div>

        <div style={styles.statusBadge}>
          <span style={styles.statusDot} />
          Standard Delivery
        </div>
      </div>

      {error && <div style={styles.errorBox}>{error}</div>}
      {zonesError && <div style={styles.errorBox}>{zonesError}</div>}

      <form onSubmit={handleSubmit}>
        <div style={styles.mainGrid}>
          {/* LEFT: Booking form */}
          <div style={styles.formColumn}>
            {/* Pickup details */}
            <section style={styles.section}>
              <div style={styles.sectionHeading}>
                <span style={styles.stepNumber}>01</span>
                <div>
                  <h2 style={styles.sectionTitle}>Pickup Details</h2>
                  <p style={styles.sectionDescription}>
                    Where should we collect your parcel?
                  </p>
                </div>
              </div>

              <div style={styles.fieldGrid}>
                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="pickupContactName">
                    Sender Name
                  </label>
                  <input
                    id="pickupContactName"
                    style={styles.input}
                    required
                    maxLength={150}
                    value={form.pickupContactName}
                    onChange={(e) =>
                      updateField('pickupContactName', e.target.value)
                    }
                    placeholder="Enter sender's full name"
                  />
                </div>

                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="pickupContactPhone">
                    Contact Number
                  </label>
                  <input
                    id="pickupContactPhone"
                    type="tel"
                    style={styles.input}
                    required
                    pattern="\+?[0-9]{7,15}"
                    value={form.pickupContactPhone}
                    onChange={(e) =>
                      updateField('pickupContactPhone', e.target.value)
                    }
                    placeholder="+94771234567"
                  />
                  <span style={styles.helperText}>
                    Include your country code if applicable.
                  </span>
                </div>

                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="pickupAddress">
                    Pickup Address
                  </label>
                  <textarea
                    id="pickupAddress"
                    style={styles.textarea}
                    required
                    maxLength={500}
                    rows={3}
                    value={form.pickupAddress}
                    onChange={(e) =>
                      updateField('pickupAddress', e.target.value)
                    }
                    placeholder="Enter the complete pickup address"
                  />
                </div>
              </div>
            </section>

            {/* Recipient details */}
            <section style={styles.section}>
              <div style={styles.sectionHeading}>
                <span style={styles.stepNumber}>02</span>
                <div>
                  <h2 style={styles.sectionTitle}>Recipient Details</h2>
                  <p style={styles.sectionDescription}>
                    Who will receive the parcel?
                  </p>
                </div>
              </div>

              <div style={styles.fieldGrid}>
                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="recipientName">
                    Recipient Name
                  </label>
                  <input
                    id="recipientName"
                    style={styles.input}
                    required
                    maxLength={150}
                    value={form.recipientName}
                    onChange={(e) =>
                      updateField('recipientName', e.target.value)
                    }
                    placeholder="Enter recipient's full name"
                  />
                </div>

                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="recipientPhone">
                    Recipient Contact Number
                  </label>
                  <input
                    id="recipientPhone"
                    type="tel"
                    style={styles.input}
                    required
                    pattern="\+?[0-9]{7,15}"
                    value={form.recipientPhone}
                    onChange={(e) =>
                      updateField('recipientPhone', e.target.value)
                    }
                    placeholder="+94719876543"
                  />
                </div>

                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="dropoffAddress">
                    Delivery Address
                  </label>
                  <textarea
                    id="dropoffAddress"
                    style={styles.textarea}
                    required
                    maxLength={500}
                    rows={3}
                    value={form.dropoffAddress}
                    onChange={(e) =>
                      updateField('dropoffAddress', e.target.value)
                    }
                    placeholder="Enter the complete delivery address"
                  />
                </div>
              </div>
            </section>

            {/* Parcel specifications */}
            <section style={styles.section}>
              <div style={styles.sectionHeading}>
                <span style={styles.stepNumber}>03</span>
                <div>
                  <h2 style={styles.sectionTitle}>Parcel Specifications</h2>
                  <p style={styles.sectionDescription}>
                    Tell us about the parcel you're sending.
                  </p>
                </div>
              </div>

              <label style={styles.label}>Package Size</label>

              <div style={styles.sizeGrid}>
                {parcelSizes.map((size) => {
                  const selected = form.sizeCategory === size.name

                  return (
                    <button
                      key={size.name}
                      type="button"
                      onClick={() =>
                        updateField('sizeCategory', size.name)
                      }
                      style={{
                        ...styles.sizeCard,
                        ...(selected ? styles.selectedSizeCard : {}),
                      }}
                    >
                      <span
                        style={{
                          ...styles.sizeIcon,
                          ...(selected ? styles.selectedSizeIcon : {}),
                        }}
                      >
                        {size.icon}
                      </span>
                      <strong style={styles.sizeName}>{size.name}</strong>
                      <span style={styles.sizeDescription}>
                        {size.description}
                      </span>
                      {selected && (
                        <span style={styles.selectedMark}>✓</span>
                      )}
                    </button>
                  )
                })}
              </div>

              <div style={styles.fieldGrid}>
                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="weightKg">
                    Parcel Weight (kg)
                  </label>
                  <input
                    id="weightKg"
                    type="number"
                    min="0.1"
                    max="50"
                    step="0.1"
                    required
                    style={styles.input}
                    value={form.weightKg}
                    onChange={(e) =>
                      updateField('weightKg', Number(e.target.value))
                    }
                  />
                  <span style={styles.helperText}>
                    Enter a weight between 0.1 kg and 50 kg.
                  </span>
                </div>

                <div style={styles.fieldFull}>
                  <label style={styles.label} htmlFor="paymentMethod">
                    Payment Method
                  </label>
                  <select
                    id="paymentMethod"
                    style={styles.input}
                    value={form.paymentMethod}
                    onChange={(e) =>
                      updateField('paymentMethod', e.target.value)
                    }
                  >
                    <option value="COD">Cash on Delivery (COD)</option>
                  </select>
                </div>
              </div>
            </section>

            {/* Delivery zone */}
            <section style={styles.section}>
              <div style={styles.sectionHeading}>
                <span style={styles.stepNumber}>04</span>
                <div>
                  <h2 style={styles.sectionTitle}>Delivery Zone</h2>
                  <p style={styles.sectionDescription}>
                    Select the delivery zone for your parcel.
                  </p>
                </div>
              </div>

              <label style={styles.label} htmlFor="zoneId">
                Delivery Zone
              </label>
              <select
                id="zoneId"
                style={styles.input}
                value={form.zoneId}
                onChange={(e) => updateField('zoneId', e.target.value)}
                disabled={zonesLoading || zones.length === 0}
                required
              >
                {zonesLoading && (
                  <option value="">Loading zones...</option>
                )}
                {!zonesLoading && zones.length === 0 && (
                  <option value="">No active zones available</option>
                )}
                {zones.map((zone) => (
                  <option key={zone.id} value={zone.id}>
                    {zone.zoneName}
                  </option>
                ))}
              </select>

              {selectedZone?.description && (
                <span style={styles.helperText}>
                  {selectedZone.description}
                </span>
              )}
            </section>
          </div>

          {/* RIGHT: Order summary */}
          <aside style={styles.summaryColumn}>
            <div style={styles.summaryCard}>
              <span style={styles.eyebrow}>BOOKING SUMMARY</span>
              <h2 style={styles.summaryTitle}>Your Delivery</h2>
              <p style={styles.sectionDescription}>
                Review your parcel details before confirming.
              </p>

              <div style={styles.summaryDivider} />

              <div style={styles.summaryRoute}>
                <div style={styles.routeMarker}>
                  <span style={styles.pickupDot} />
                  <span style={styles.routeLine} />
                  <span style={styles.dropoffDot} />
                </div>

                <div style={styles.routeDetails}>
                  <div>
                    <span style={styles.routeLabel}>PICKUP FROM</span>
                    <strong style={styles.routeValue}>
                      {form.pickupContactName || 'Sender address'}
                    </strong>
                    <span style={styles.routeSubValue}>
                      {form.pickupAddress || 'Pickup address not entered'}
                    </span>
                  </div>

                  <div style={styles.routeDestination}>
                    <span style={styles.routeLabel}>DELIVER TO</span>
                    <strong style={styles.routeValue}>
                      {form.recipientName || 'Recipient'}
                    </strong>
                    <span style={styles.routeSubValue}>
                      {form.dropoffAddress || 'Delivery address not entered'}
                    </span>
                  </div>
                </div>
              </div>

              <div style={styles.summaryDivider} />

              <div style={styles.summaryRow}>
                <span>Delivery zone</span>
                <strong>{selectedZone?.zoneName || 'Not selected'}</strong>
              </div>

              <div style={styles.summaryRow}>
                <span>Package size</span>
                <strong>{form.sizeCategory}</strong>
              </div>

              <div style={styles.summaryRow}>
                <span>Parcel weight</span>
                <strong>
                  {form.weightKg > 0 ? `${form.weightKg} kg` : '—'}
                </strong>
              </div>

              <div style={styles.summaryRow}>
                <span>Payment method</span>
                <strong>Cash on Delivery</strong>
              </div>

              <div style={styles.summaryDivider} />

              <div style={styles.totalRow}>
                <span>Estimated delivery fee</span>
                <strong>
                  {feeLoading
                    ? 'Calculating...'
                    : estimatedFee !== null
                      ? `LKR ${estimatedFee.toFixed(2)}`
                      : 'Not available'}
                </strong>
              </div>

              {feeError && (
                <p style={styles.feeError}>{feeError}</p>
              )}

              <p style={styles.summaryNote}>
                The fee is calculated using the selected delivery zone
                and parcel weight.
              </p>

              <button
                type="submit"
                disabled={bookingDisabled}
                style={{
                  ...styles.confirmButton,
                  opacity: bookingDisabled ? 0.55 : 1,
                  cursor: bookingDisabled ? 'not-allowed' : 'pointer',
                }}
              >
                {loading ? 'Submitting Booking...' : 'Confirm & Book Delivery'}
                {!loading && <span>→</span>}
              </button>

              <div style={styles.secureNote}>
                <span style={styles.secureIcon}>✓</span>
                Your booking will be submitted securely.
              </div>
            </div>
          </aside>
        </div>
      </form>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: {
    width: '100%',
    maxWidth: 1200,
    margin: '0 auto',
    paddingBottom: 40,
    color: '#172033',
  },
  pageHeading: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: 16,
    marginBottom: 28,
  },
  eyebrow: {
    display: 'block',
    color: '#2563EB',
    fontSize: '0.72rem',
    fontWeight: 750,
    letterSpacing: '0.1em',
    marginBottom: 9,
  },
  pageTitle: {
    color: '#14213D',
    fontSize: 'clamp(1.6rem, 3vw, 2rem)',
    fontWeight: 750,
    letterSpacing: '-0.04em',
    margin: '0 0 8px',
  },
  description: {
    color: '#64748B',
    lineHeight: 1.6,
    margin: 0,
    fontSize: '0.92rem',
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    padding: '9px 13px',
    background: '#EFF6FF',
    border: '1px solid #DBEAFE',
    color: '#1D4ED8',
    borderRadius: 30,
    fontSize: '0.78rem',
    fontWeight: 650,
  },
  statusDot: {
    width: 8,
    height: 8,
    borderRadius: '50%',
    background: '#2563EB',
  },
  mainGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 340px), 1fr))',
    gap: 24,
    alignItems: 'start',
  },
  formColumn: {
    display: 'flex',
    flexDirection: 'column',
    gap: 18,
    minWidth: 0,
  },
  section: {
    background: '#FFFFFF',
    border: '1px solid #E2E8F0',
    borderRadius: 16,
    padding: 'clamp(18px, 3vw, 25px)',
    boxShadow: '0 4px 15px rgba(15, 23, 42, 0.025)',
  },
  sectionHeading: {
    display: 'flex',
    alignItems: 'center',
    gap: 13,
    marginBottom: 22,
  },
  stepNumber: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    width: 39,
    height: 39,
    borderRadius: 11,
    background: '#EFF6FF',
    color: '#2563EB',
    fontSize: '0.8rem',
    fontWeight: 750,
  },
  sectionTitle: {
    color: '#172033',
    fontSize: '1.02rem',
    fontWeight: 700,
    margin: '0 0 4px',
  },
  sectionDescription: {
    color: '#8490A5',
    fontSize: '0.8rem',
    margin: 0,
    lineHeight: 1.5,
  },
  fieldGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 220px), 1fr))',
    gap: '14px 16px',
  },
  fieldFull: {
    minWidth: 0,
  },
  label: {
    display: 'block',
    color: '#344054',
    fontSize: '0.82rem',
    fontWeight: 650,
    marginBottom: 7,
  },
  input: {
    width: '100%',
    boxSizing: 'border-box',
    padding: '12px 13px',
    borderRadius: 9,
    border: '1px solid #DCE3ED',
    background: '#FFFFFF',
    color: '#172033',
    fontSize: '0.88rem',
    outlineColor: '#2563EB',
  },
  textarea: {
    width: '100%',
    boxSizing: 'border-box',
    padding: '12px 13px',
    borderRadius: 9,
    border: '1px solid #DCE3ED',
    background: '#FFFFFF',
    color: '#172033',
    fontSize: '0.88rem',
    fontFamily: 'inherit',
    resize: 'vertical',
    outlineColor: '#2563EB',
  },
  helperText: {
    display: 'block',
    color: '#8490A5',
    fontSize: '0.75rem',
    marginTop: 6,
    lineHeight: 1.5,
  },
  sizeGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(110px, 1fr))',
    gap: 10,
    marginBottom: 20,
  },
  sizeCard: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-start',
    gap: 7,
    padding: '14px 12px',
    border: '1px solid #E2E8F0',
    borderRadius: 12,
    background: '#FFFFFF',
    color: '#344054',
    textAlign: 'left',
    cursor: 'pointer',
  },
  selectedSizeCard: {
    border: '1.5px solid #2563EB',
    background: '#F5F8FF',
  },
  sizeIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: 35,
    height: 35,
    borderRadius: 9,
    background: '#F1F5F9',
    color: '#64748B',
    fontSize: '1.4rem',
  },
  selectedSizeIcon: {
    background: '#DBEAFE',
    color: '#2563EB',
  },
  sizeName: {
    fontSize: '0.87rem',
    color: '#172033',
  },
  sizeDescription: {
    fontSize: '0.7rem',
    color: '#8490A5',
    lineHeight: 1.4,
  },
  selectedMark: {
    position: 'absolute',
    top: 9,
    right: 9,
    color: '#2563EB',
    fontWeight: 800,
    fontSize: '0.8rem',
  },
  summaryColumn: {
    minWidth: 0,
  },
  summaryCard: {
    background: '#FFFFFF',
    border: '1px solid #E2E8F0',
    borderRadius: 16,
    padding: 'clamp(20px, 3vw, 26px)',
    boxShadow: '0 8px 28px rgba(15, 23, 42, 0.045)',
  },
  summaryTitle: {
    color: '#172033',
    fontSize: '1.25rem',
    fontWeight: 750,
    margin: '0 0 6px',
  },
  summaryDivider: {
    height: 1,
    background: '#E9EDF3',
    margin: '20px 0',
  },
  summaryRoute: {
    display: 'flex',
    gap: 13,
    marginTop: 22,
  },
  routeMarker: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    paddingTop: 4,
    flexShrink: 0,
  },
  pickupDot: {
    width: 11,
    height: 11,
    border: '3px solid #2563EB',
    borderRadius: '50%',
    boxSizing: 'border-box',
  },
  routeLine: {
    width: 1,
    height: 43,
    borderLeft: '2px dashed #CBD5E1',
  },
  dropoffDot: {
    width: 11,
    height: 11,
    border: '3px solid #16A34A',
    borderRadius: '3px',
    boxSizing: 'border-box',
  },
  routeDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: 20,
    minWidth: 0,
  },
  routeDestination: {
    marginTop: 0,
  },
  routeLabel: {
    display: 'block',
    color: '#94A3B8',
    fontSize: '0.67rem',
    fontWeight: 750,
    letterSpacing: '0.08em',
    marginBottom: 5,
  },
  routeValue: {
    display: 'block',
    color: '#26344D',
    fontSize: '0.86rem',
    marginBottom: 4,
    overflowWrap: 'anywhere',
  },
  routeSubValue: {
    display: 'block',
    color: '#8490A5',
    fontSize: '0.77rem',
    lineHeight: 1.5,
    overflowWrap: 'anywhere',
  },
  summaryRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: 12,
    marginBottom: 14,
    color: '#64748B',
    fontSize: '0.82rem',
  },
  totalRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 12,
    color: '#344054',
    fontSize: '0.88rem',
    fontWeight: 650,
  },
  feeError: {
    color: '#DC2626',
    fontSize: '0.78rem',
    marginTop: 10,
  },
  summaryNote: {
    color: '#8490A5',
    fontSize: '0.74rem',
    lineHeight: 1.55,
    margin: '15px 0 18px',
  },
  confirmButton: {
    width: '100%',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    gap: 10,
    padding: '14px 16px',
    border: 'none',
    borderRadius: 10,
    background: '#2563EB',
    color: '#FFFFFF',
    fontSize: '0.87rem',
    fontWeight: 700,
    cursor: 'pointer',
  },
  secureNote: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    gap: 7,
    color: '#8490A5',
    fontSize: '0.72rem',
    marginTop: 14,
  },
  secureIcon: {
    color: '#16A34A',
    fontWeight: 800,
  },
  errorBox: {
    background: '#FEF2F2',
    border: '1px solid #FECACA',
    color: '#B91C1C',
    padding: '12px 15px',
    borderRadius: 10,
    marginBottom: 18,
    fontSize: '0.85rem',
  },
  successPage: {
    maxWidth: 650,
    margin: '35px auto',
  },
  successCard: {
    background: '#FFFFFF',
    border: '1px solid #E2E8F0',
    borderRadius: 18,
    padding: 'clamp(24px, 5vw, 40px)',
    textAlign: 'center',
    boxShadow: '0 12px 35px rgba(15, 23, 42, 0.05)',
  },
  successIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: 55,
    height: 55,
    borderRadius: '50%',
    background: '#DCFCE7',
    color: '#15803D',
    fontSize: '1.7rem',
    fontWeight: 800,
    margin: '0 auto 17px',
  },
  successBadge: {
    display: 'inline-block',
    background: '#DCFCE7',
    color: '#15803D',
    fontSize: '0.72rem',
    fontWeight: 750,
    letterSpacing: '0.07em',
    padding: '7px 13px',
    borderRadius: 30,
  },
  successTitle: {
    color: '#172033',
    fontSize: '1.7rem',
    margin: '18px 0 10px',
  },
  trackingBox: {
    background: '#F1F5F9',
    borderRadius: 12,
    padding: 20,
    margin: '24px 0',
  },
  smallLabel: {
    color: '#64748B',
    fontSize: '0.72rem',
    fontWeight: 700,
    letterSpacing: '0.08em',
  },
  trackingNumber: {
    color: '#2563EB',
    fontSize: '1.5rem',
    overflowWrap: 'anywhere',
    margin: '9px 0 0',
  },
  confirmedFee: {
    display: 'flex',
    justifyContent: 'space-between',
    gap: 12,
    color: '#475569',
    fontSize: '0.9rem',
    marginBottom: 20,
  },
  actions: {
    display: 'flex',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: 12,
    marginTop: 24,
  },
  primaryButton: {
    background: '#2563EB',
    color: '#FFFFFF',
    border: 'none',
    padding: '12px 20px',
    borderRadius: 10,
    fontWeight: 650,
    cursor: 'pointer',
  },
  secondaryButton: {
    background: '#F1F5F9',
    color: '#172033',
    border: '1px solid #CBD5E1',
    padding: '12px 20px',
    borderRadius: 10,
    fontWeight: 650,
    cursor: 'pointer',
  },
}
