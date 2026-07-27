import { Fragment, useState, useMemo } from 'react';
import type { ReportNode } from '@/types';
import { ChevronDown, ChevronRight, ChevronsDown, ChevronsUp } from 'lucide-react';
import { Button } from '@/components/ui/button';

const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

function formatAmount(amount: number | null): string {
  if (amount === null || amount === undefined) return '—';
  return currencyFormatter.format(amount);
}

function getAllParentPaths(nodes: ReportNode[]): string[] {
  const paths: string[] = [];
  function walk(nodeList: ReportNode[]) {
    for (const node of nodeList) {
      if (node.children && node.children.length > 0) {
        paths.push(node.rowPath);
        walk(node.children);
      }
    }
  }
  walk(nodes);
  return paths;
}

interface ReportRowProps {
  node: ReportNode;
  collapsedPaths: Set<string>;
  onToggleCollapse: (rowPath: string) => void;
}

function ReportRow({ node, collapsedPaths, onToggleCollapse }: ReportRowProps) {
  const hasChildren = node.children && node.children.length > 0;
  const isCollapsed = collapsedPaths.has(node.rowPath);
  const isSection = node.rowType === 'Section';
  const isNegative = (node.amount ?? 0) < 0;

  return (
    <Fragment>
      <tr
        className={`${isSection ? 'bg-muted/40 font-semibold' : 'hover:bg-muted/30 transition-colors'
          } ${hasChildren ? 'cursor-pointer select-none' : ''}`}
        onClick={() => {
          if (hasChildren) onToggleCollapse(node.rowPath);
        }}
      >
        <td className="py-2 pr-4">
          <div
            className="flex items-center gap-1.5"
            style={{ paddingLeft: `${node.depth * 1.25}rem` }}
          >
            {hasChildren ? (
              <button
                type="button"
                className="p-0.5 rounded hover:bg-muted/60 text-muted-foreground hover:text-foreground transition-colors shrink-0"
                onClick={(e) => {
                  e.stopPropagation();
                  onToggleCollapse(node.rowPath);
                }}
                aria-label={isCollapsed ? 'Expand section' : 'Collapse section'}
              >
                {isCollapsed ? (
                  <ChevronRight className="h-4 w-4" />
                ) : (
                  <ChevronDown className="h-4 w-4" />
                )}
              </button>
            ) : (
              <span className="w-5 shrink-0" />
            )}
            <span
              className={`truncate ${isSection ? 'font-semibold text-foreground' : 'text-muted-foreground font-normal'
                }`}
              title={node.label ?? undefined}
            >
              {node.label ?? '—'}
            </span>
          </div>
        </td>
        <td
          className={`py-2 pl-4 text-right tabular-nums whitespace-nowrap ${isSection ? 'font-semibold text-foreground' : 'text-muted-foreground'
            } ${isNegative ? 'text-destructive' : ''}`}
        >
          {formatAmount(node.amount)}
        </td>
      </tr>
      {!isCollapsed &&
        hasChildren &&
        node.children.map((child) => (
          <ReportRow
            key={child.rowPath}
            node={child}
            collapsedPaths={collapsedPaths}
            onToggleCollapse={onToggleCollapse}
          />
        ))}
    </Fragment>
  );
}

export function ReportTreeView({ rows, children }: { rows: ReportNode[]; children?: React.ReactNode }) {
  const allParentPaths = useMemo(() => getAllParentPaths(rows), [rows]);
  const [collapsedPaths, setCollapsedPaths] = useState<Set<string>>(new Set());

  const toggleCollapse = (rowPath: string) => {
    setCollapsedPaths((prev) => {
      const next = new Set(prev);
      if (next.has(rowPath)) {
        next.delete(rowPath);
      } else {
        next.add(rowPath);
      }
      return next;
    });
  };

  const expandAll = () => setCollapsedPaths(new Set());
  const collapseAll = () => setCollapsedPaths(new Set(allParentPaths));

  if (rows.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-muted-foreground">
        No data for this period.
      </p>
    );
  }

  return (
    <div className="space-y-3">
      {/* Header containing children and collapse/expand controls */}
      {(children || allParentPaths.length > 0) && (
        <div className="flex items-center justify-between gap-4 pb-1">
          <div>{children}</div>
          {allParentPaths.length > 0 && (
            <div className="flex items-center gap-2 shrink-0">
              <Button
                variant="ghost"
                size="sm"
                onClick={expandAll}
                className="h-7 text-xs text-muted-foreground hover:text-foreground"
              >
                <ChevronsDown className="h-3.5 w-3.5 mr-1" />
                Expand All
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={collapseAll}
                className="h-7 text-xs text-muted-foreground hover:text-foreground"
              >
                <ChevronsUp className="h-3.5 w-3.5 mr-1" />
                Collapse All
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Report Table */}
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
              <ReportRow
                key={node.rowPath}
                node={node}
                collapsedPaths={collapsedPaths}
                onToggleCollapse={toggleCollapse}
              />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
