import { accountTypes, type AccountType } from '../api/accounts'

export function AccountTypeSelect({
  id,
  label,
  value,
  onChange,
}: {
  id?: string
  label?: string
  value: AccountType
  onChange: (type: AccountType) => void
}) {
  return (
    <select id={id} aria-label={label} value={value} onChange={(e) => onChange(e.target.value as AccountType)}>
      {accountTypes.map((type) => (
        <option key={type} value={type}>
          {type}
        </option>
      ))}
    </select>
  )
}
