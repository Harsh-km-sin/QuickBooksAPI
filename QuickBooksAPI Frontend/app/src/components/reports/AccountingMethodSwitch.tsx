export type AccountingMethod = 'Accrual' | 'Cash';

interface AccountingMethodSwitchProps {
  value: AccountingMethod;
  onChange: (method: AccountingMethod) => void;
  disabled?: boolean;
}

export function AccountingMethodSwitch({
  value,
  onChange,
  disabled = false,
}: AccountingMethodSwitchProps) {
  return (
    <div className="flex flex-col space-y-2">
      <span className="text-xs font-medium text-muted-foreground">Accounting method</span>
      <div className="inline-flex h-9 items-center rounded-lg bg-muted p-1 text-muted-foreground border border-border">
        <button
          type="button"
          disabled={disabled}
          onClick={() => onChange('Cash')}
          className={`inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-xs font-semibold ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 ${
            value === 'Cash'
              ? 'bg-background text-foreground shadow-sm border border-border'
              : 'hover:text-foreground'
          }`}
        >
          Cash
        </button>
        <button
          type="button"
          disabled={disabled}
          onClick={() => onChange('Accrual')}
          className={`inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-xs font-semibold ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 ${
            value === 'Accrual'
              ? 'bg-background text-foreground shadow-sm border border-border'
              : 'hover:text-foreground'
          }`}
        >
          Accrual
        </button>
      </div>
    </div>
  );
}
