import { useEffect, useId, useState, type Ref } from 'react'
import { COURIER_USER_SEARCH_DEBOUNCE_MS } from '../../../lib/constants'
import { useCourierUsers, type UserDto } from '../../identity'
import { isDuplicateCourier } from '../utils/courierWorkload.utils'
import type { CourierDto } from '../types/dispatch.types'

const EMPTY_USERS: UserDto[] = []

export function CourierUserCombobox({ couriers, selected, onSelect, disabled, duplicate, describedBy, inputRef }: {
  couriers: CourierDto[]
  selected: UserDto | null
  onSelect: (user: UserDto | null) => void
  disabled?: boolean
  duplicate?: boolean
  describedBy?: string
  inputRef?: Ref<HTMLInputElement>
}) {
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(0)
  const usersQuery = useCourierUsers(query, open && !disabled)
  const users = usersQuery.data?.items ?? EMPTY_USERS

  useEffect(() => {
    const timer = window.setTimeout(() => setQuery(search), COURIER_USER_SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(timer)
  }, [search])

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (disabled) return
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setOpen(true)
      setActiveIndex((index) => users.length ? (index + 1) % users.length : 0)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setOpen(true)
      setActiveIndex((index) => users.length ? (index - 1 + users.length) % users.length : 0)
    } else if (event.key === 'Enter' && open && users[activeIndex]) {
      event.preventDefault()
      onSelect(users[activeIndex])
      setOpen(false)
    } else if (event.key === 'Escape' && open) {
      event.stopPropagation()
      setOpen(false)
    }
  }

  const showOptions = open && !disabled
  return <div className="courier-user-combobox">
    <label htmlFor={`${listId}-input`}>Select user account <span aria-hidden="true">*</span></label>
    <span className="courier-field-hint">Courier accounts only</span>
    <div className={`courier-combobox-control${duplicate ? ' is-invalid' : ''}`}>
      <span aria-hidden="true">⌕</span>
        <input
        ref={inputRef}
        id={`${listId}-input`}
        role="combobox"
        aria-autocomplete="list"
        aria-expanded={showOptions}
        aria-controls={`${listId}-listbox`}
        aria-activedescendant={showOptions && users.length ? `${listId}-option-${users[activeIndex % users.length].id}` : undefined}
        aria-required="true"
        aria-invalid={duplicate || undefined}
        aria-describedby={describedBy}
        value={selected && !open ? selected.fullName : search}
        placeholder="Search by name or email address…"
        disabled={disabled}
        onFocus={() => { setOpen(true); setSearch(''); setQuery('') }}
        onChange={(event) => { setSearch(event.target.value); setActiveIndex(0); onSelect(null); setOpen(true) }}
        onKeyDown={handleKeyDown}
      />
      <button type="button" aria-label={showOptions ? 'Close courier accounts' : 'Open courier accounts'} aria-expanded={showOptions} disabled={disabled} onClick={() => setOpen((value) => !value)}>⌄</button>
    </div>
    {showOptions && <div className="courier-combobox-popover">
      <ul id={`${listId}-listbox`} role="listbox" aria-label="Active courier accounts">
        {usersQuery.isLoading && <li className="courier-user-state" role="presentation">Searching courier accounts…</li>}
        {usersQuery.isError && <li className="courier-user-state" role="presentation">Couldn't load courier accounts. <button type="button" onClick={() => void usersQuery.refetch()}>Try again</button></li>}
        {!usersQuery.isLoading && !usersQuery.isError && users.length === 0 && <li className="courier-user-state" role="presentation">No active courier accounts found.</li>}
        {!usersQuery.isLoading && !usersQuery.isError && users.map((user, index) => {
          const registered = isDuplicateCourier(user.id, couriers)
          return <li
            id={`${listId}-option-${user.id}`}
            key={user.id}
            role="option"
            aria-selected={selected?.id === user.id}
            className={index === activeIndex ? 'is-active' : ''}
            onMouseEnter={() => setActiveIndex(index)}
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => { onSelect(user); setOpen(false) }}
          >
            <span><strong>{user.fullName}</strong><small>{user.email}</small></span>
            <em className={registered ? 'is-registered' : ''}>{registered ? 'Registered' : 'Ready'}</em>
          </li>
        })}
      </ul>
    </div>}
  </div>
}
