import type { ReactNode } from 'react';
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import type { SortDirection } from '@/types';

export interface DataTableColumn<T> {
  key: string;
  header: ReactNode;
  sortable?: boolean;
  headerClassName?: string;
  cellClassName?: string;
  render: (row: T) => ReactNode;
}

export interface DataTableSort {
  sortBy: string;
  sortDir: SortDirection;
}

export interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  data: T[];
  rowKey: (row: T) => string | number;
  isLoading?: boolean;
  sort?: DataTableSort;
  onSortChange?: (sort: DataTableSort) => void;
  emptyState?: ReactNode;
  className?: string;
  skeletonRows?: number;
}

/**
 * Sortable, paginatable-by-caller data table. Only this component's own body
 * scrolls (`overflow-auto` with a sticky header) — it never scrolls the page
 * around it, so it composes into a fixed-height card alongside a search bar
 * and pagination footer that stay put.
 */
export function DataTable<T>({
  columns,
  data,
  rowKey,
  isLoading,
  sort,
  onSortChange,
  emptyState,
  className,
  skeletonRows = 8,
}: DataTableProps<T>) {
  const handleSort = (column: DataTableColumn<T>) => {
    if (!column.sortable || !onSortChange) return;
    if (sort?.sortBy === column.key) {
      onSortChange({ sortBy: column.key, sortDir: sort.sortDir === 'asc' ? 'desc' : 'asc' });
    } else {
      onSortChange({ sortBy: column.key, sortDir: 'asc' });
    }
  };

  if (!isLoading && data.length === 0) {
    return <div className={cn('flex-1 overflow-auto', className)}>{emptyState}</div>;
  }

  return (
    <div className={cn('flex-1 min-h-0 overflow-auto', className)}>
      <Table>
        <TableHeader className="sticky top-0 z-10 bg-card">
          <TableRow>
            {columns.map((column) => (
              <TableHead key={column.key} className={column.headerClassName}>
                {column.sortable ? (
                  <button
                    type="button"
                    onClick={() => handleSort(column)}
                    className="flex items-center gap-1 hover:text-foreground"
                  >
                    {column.header}
                    {sort?.sortBy === column.key ? (
                      sort.sortDir === 'asc' ? (
                        <ArrowUp className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowDown className="h-3.5 w-3.5" />
                      )
                    ) : (
                      <ArrowUpDown className="h-3.5 w-3.5 text-muted-foreground/50" />
                    )}
                  </button>
                ) : (
                  column.header
                )}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading
            ? Array.from({ length: skeletonRows }).map((_, i) => (
                <TableRow key={i}>
                  {columns.map((column) => (
                    <TableCell key={column.key}>
                      <Skeleton className="h-4 w-full max-w-[160px]" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            : data.map((row) => (
                <TableRow key={rowKey(row)}>
                  {columns.map((column) => (
                    <TableCell key={column.key} className={column.cellClassName}>
                      {column.render(row)}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
        </TableBody>
      </Table>
    </div>
  );
}
