import { readableDate } from './format'

/**
 * One row as the app would store it: date, description, amount. The same three figures at both
 * stations — the Map step's rehearsal and station 3's performance — so a row cannot read one way
 * while it is being mapped and another way when it is being confirmed.
 */
export function ReadingLine({
  row,
}: {
  row: { date: string | null; description: string | null; amount: number | null }
}) {
  return (
    <>
      <span className="reading-date">
        {row.date ? readableDate(row.date) : <span className="reading-missing">no date</span>}
      </span>
      <span className="reading-description">
        {row.description ?? <span className="reading-missing">no description</span>}
      </span>
      <span className="reading-amount">
        {row.amount === null ? <span className="reading-missing">no amount</span> : row.amount.toFixed(2)}
      </span>
    </>
  )
}
