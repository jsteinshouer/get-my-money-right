import { needWants, type NeedWant } from '../api/transactions'

export function NeedWantSelect({
  id,
  label,
  value,
  onChange,
  includeAllOption = false,
  required = false,
  placeholder = 'Select…',
}: {
  id?: string
  label?: string
  value: NeedWant | ''
  onChange: (value: NeedWant | '') => void
  includeAllOption?: boolean
  required?: boolean
  /** What the empty choice reads as, when there is no "all" option to show instead. */
  placeholder?: string
}) {
  return (
    <select
      id={id}
      aria-label={label}
      value={value}
      required={required}
      onChange={(e) => onChange(e.target.value as NeedWant | '')}
    >
      <option value="" disabled={required && !includeAllOption}>
        {includeAllOption ? 'All' : placeholder}
      </option>
      {needWants.map((needWant) => (
        <option key={needWant} value={needWant}>
          {needWant}
        </option>
      ))}
    </select>
  )
}
