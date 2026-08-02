import type { Account } from '../api/accounts'

export function AccountSelect({
  id,
  label,
  accounts,
  value,
  onChange,
  includeAllOption = false,
  required = false,
}: {
  id?: string
  label?: string
  accounts: Account[]
  value: number | ''
  onChange: (value: number | '') => void
  includeAllOption?: boolean
  required?: boolean
}) {
  return (
    <select
      id={id}
      aria-label={label}
      value={value}
      required={required}
      onChange={(e) => onChange(e.target.value === '' ? '' : Number(e.target.value))}
    >
      <option value="" disabled={required && !includeAllOption}>
        {includeAllOption ? 'All accounts' : 'Select an account'}
      </option>
      {accounts.map((account) => (
        <option key={account.id} value={account.id}>
          {account.name}
          {account.isActive ? '' : ' (inactive)'}
        </option>
      ))}
    </select>
  )
}
