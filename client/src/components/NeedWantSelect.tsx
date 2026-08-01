import { needWants, type NeedWant } from '../api/transactions'

export function NeedWantSelect({
  id,
  label,
  value,
  onChange,
  includeAllOption = false,
  required = false,
}: {
  id?: string
  label?: string
  value: NeedWant | ''
  onChange: (value: NeedWant | '') => void
  includeAllOption?: boolean
  required?: boolean
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
        {includeAllOption ? 'All' : 'Select…'}
      </option>
      {needWants.map((needWant) => (
        <option key={needWant} value={needWant}>
          {needWant}
        </option>
      ))}
    </select>
  )
}
