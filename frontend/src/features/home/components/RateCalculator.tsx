import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { pricingService, type ServiceType } from '../../../services/pricing.service'
import { ArrowRightIcon } from '../../identity/components/Icons'

const MIN_KG = 0.5
const MAX_KG = 30

const formatLkr = (amount: number) =>
  `LKR ${amount.toLocaleString('en-LK', { maximumFractionDigits: 0 })}`

/** Live price estimate from the Pricing module: GET /zones and GET /zones/{id}/estimate. */
export function RateCalculator() {
  const zones = useQuery({ queryKey: ['zones'], queryFn: pricingService.getZones })
  const activeZones = zones.data?.filter((z) => z.isActive) ?? []

  const [chosenZoneId, setChosenZoneId] = useState('')
  const [serviceType, setServiceType] = useState<ServiceType>('Standard')
  const [weight, setWeight] = useState(1.5)
  const zoneId = chosenZoneId || activeZones[0]?.id || ''

  const estimate = useQuery({
    queryKey: ['estimate', zoneId, weight, serviceType],
    queryFn: () => pricingService.estimate(zoneId, weight, serviceType),
    enabled: zoneId !== '',
    placeholderData: (previous) => previous,
  })

  let rateText = '—'
  if (estimate.data) rateText = formatLkr(estimate.data.total)
  else if (zones.isError || estimate.isError) rateText = 'Unavailable'

  return (
    <div className="sd-card sd-rate-card">
      <div className="sd-rate-card__row">
        <div className="sd-field">
          <label htmlFor="rate-zone">Delivery zone</label>
          <div className="sd-select">
            <select id="rate-zone" value={zoneId} onChange={(e) => setChosenZoneId(e.target.value)}
              disabled={activeZones.length === 0}>
              {activeZones.length === 0 && <option value="">{zones.isLoading ? 'Loading zones…' : 'No zones available'}</option>}
              {activeZones.map((zone) => (
                <option key={zone.id} value={zone.id}>{zone.zoneName}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="sd-field">
          <span className="sd-field__label" id="rate-service">Service</span>
          <div className="sd-segmented" role="radiogroup" aria-labelledby="rate-service">
            {(['Standard', 'Express'] as const).map((type) => (
              <button key={type} type="button" role="radio" aria-checked={serviceType === type}
                className={serviceType === type ? 'is-active' : ''} onClick={() => setServiceType(type)}>
                {type}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="sd-field">
        <div className="sd-field__label-row">
          <label htmlFor="rate-weight">Package Weight</label>
          <span className="sd-mono sd-text-primary">{weight.toFixed(1)} kg</span>
        </div>
        <input id="rate-weight" className="sd-range" type="range" min={MIN_KG} max={MAX_KG} step={0.5}
          value={weight} onChange={(e) => setWeight(Number(e.target.value))}
          style={{ ['--fill' as string]: `${((weight - MIN_KG) / (MAX_KG - MIN_KG)) * 100}%` }} />
        <div className="sd-range__scale">
          <span>0.5 kg (Envelope)</span><span>15 kg</span><span>30 kg (Carton)</span>
        </div>
      </div>

      <div className="sd-rate-result">
        <div>
          <span className="sd-rate-result__label">Estimated Delivery Rate</span>
          <strong className="sd-rate-result__amount" aria-live="polite">{rateText}</strong>
        </div>
        <Link to="/send-parcel" className="sd-btn sd-btn--primary">Book Pickup <ArrowRightIcon /></Link>
      </div>
    </div>
  )
}
