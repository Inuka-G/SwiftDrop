import { formatDateTime } from '../../../lib/utils'
import type { ParcelStatus, StatusHistoryEntry } from '../types/courier.types'
import { Icon } from './Icon'
import { MILESTONES, MILESTONE_LABELS } from './statusMeta'

interface Props {
  status: ParcelStatus
  history: StatusHistoryEntry[]
}

/** The lifecycle as a vertical list of stages, each with its time and courier note from the status history. */
export function JourneyMilestones({ status, history }: Props) {
  const failure = status === 'Failed' ? history.findLast((h) => h.toStatus === 'Failed') : undefined
  // A failed parcel stops at the stage it failed from; Failed replaces the next stage.
  const stages: ParcelStatus[] = failure
    ? [...MILESTONES.slice(0, MILESTONES.indexOf(failure.fromStatus ?? 'Created') + 1), 'Failed']
    : MILESTONES

  return (
    <ol className="milestones">
      {stages.map((stage, index) => {
        const entry = history.findLast((h) => h.toStatus === stage)
        const state = stage === 'Failed' ? 'failed' : entry ? 'done' : 'todo'
        return (
          <li key={stage} className={`milestone milestone-${state}`}>
            <span className="milestone-dot">
              {state === 'done' && <Icon name="check" size={14} />}
              {state === 'failed' && <Icon name="alert" size={14} />}
            </span>
            <div className="milestone-body">
              <p className="milestone-stage">Stage {index + 1}</p>
              <p className="milestone-title">{MILESTONE_LABELS[stage]}</p>
              {entry && (
                <time className="mono milestone-time" dateTime={entry.occurredAtUtc}>
                  {formatDateTime(entry.occurredAtUtc)}
                </time>
              )}
              {entry?.note && <p className="milestone-note">{entry.note}</p>}
            </div>
          </li>
        )
      })}
    </ol>
  )
}
