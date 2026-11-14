import { useEffect, type KeyboardEvent, type RefObject } from 'react'

export function useAccessibleDialog(dialogRef: RefObject<HTMLElement | null>, initialFocusRef: RefObject<HTMLElement | null>, opener: HTMLElement | null, submitting: boolean, onClose: () => void) {
  useEffect(() => {
    const root = document.getElementById('root')
    if (root) root.inert = true
    initialFocusRef.current?.focus()
    return () => {
      if (root) root.inert = false
      window.setTimeout(() => opener?.focus(), 0)
    }
  }, [initialFocusRef, opener])

  return function handleDialogKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === 'Escape' && !submitting) {
      event.preventDefault()
      onClose()
      return
    }
    if (event.key !== 'Tab') return
    const focusable = dialogRef.current?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), [tabindex="0"]')
    if (!focusable?.length) return
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }
}
