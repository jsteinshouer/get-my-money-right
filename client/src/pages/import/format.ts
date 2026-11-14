const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** The server hands back an ISO date; a reader wants to see the month said out loud. */
export function readableDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return `${MONTHS[month - 1]} ${day}, ${year}`
}

export function readableTimestamp(iso: string): string {
  return readableDate(iso.slice(0, 10))
}
