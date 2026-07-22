import { Fragment } from 'react';
import type { ReportNode } from '@/types';

const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

function formatAmount(amount: number | null): string {
  // A null amount means QuickBooks returned an empty cell; rendering 0 would assert a figure
  // it never reported.
  if (amount === null || amount === undefined) return '—';
  return currencyFormatter.format(amount);
}

function ReportRow({ node }: { node: ReportNode }) {
  const isSection = node.rowType === 'Section';
  const isNegative = (node.amount ?? 0) < 0;

  return (
    <Fragment>
      <tr className={isSection ? 'bg-muted/40' : 'hover:bg-muted/30 transition-colors'}>
        <td className="py-2 pr-4">
          <span
            className={`block truncate ${isSection ? 'font-semibold text-foreground' : 'text-muted-foreground'}`}
            style={{ paddingLeft: `${node.depth * 1.25}rem` }}
            title={node.label ?? undefined}
          >
            {node.label ?? '—'}
          </span>
        </td>
        <td
          className={`py-2 pl-4 text-right tabular-nums whitespace-nowrap ${
            isSection ? 'font-semibold text-foreground' : 'text-muted-foreground'
          } ${isNegative ? 'text-destructive' : ''}`}
        >
          {formatAmount(node.amount)}
        </td>
      </tr>
      {node.children.map((child) => (
        <ReportRow key={child.rowPath} node={child} />
      ))}
    </Fragment>
  );
}

export function ReportTreeView({ rows }: { rows: ReportNode[] }) {
  if (rows.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-muted-foreground">
        No data for this period.
      </p>
    );
  }

  return (
    // Wide reports scroll inside their own container rather than pushing the page sideways.
    <div className="overflow-x-auto">
      <table className="w-full min-w-[24rem] text-sm border-collapse">
        <thead>
          <tr className="border-b border-border text-xs uppercase tracking-wide text-muted-foreground">
            <th className="py-2 pr-4 text-left font-semibold">Account</th>
            <th className="py-2 pl-4 text-right font-semibold">Amount</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((node) => (
            <ReportRow key={node.rowPath} node={node} />
          ))}
        </tbody>
      </table>
    </div>
  );
}
