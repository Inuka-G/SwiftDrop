export function formatDateTime(value: string | Date): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(value instanceof Date ? value : new Date(value))
}
