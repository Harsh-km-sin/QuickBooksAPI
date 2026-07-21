import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

const DEFAULT_PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;

export interface DataTablePaginationProps {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  pageSizeOptions?: readonly number[];
  className?: string;
}

export function DataTablePagination({
  currentPage,
  pageSize,
  totalCount,
  totalPages,
  hasNextPage,
  hasPreviousPage,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZE_OPTIONS,
  className,
}: DataTablePaginationProps) {
  if (totalCount === 0) return null;

  return (
    <div className={cn('flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 border-t px-4 py-3', className)}>
      <div className="flex items-center gap-4">
        <p className="text-sm text-muted-foreground">
          Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
        </p>
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground whitespace-nowrap">Per page</span>
          <Select value={String(pageSize)} onValueChange={(value) => onPageSizeChange(Number(value))}>
            <SelectTrigger className="w-[70px]" size="sm">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {pageSizeOptions.map((size) => (
                <SelectItem key={size} value={String(size)}>
                  {size}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" onClick={() => onPageChange(currentPage - 1)} disabled={!hasPreviousPage}>
          <ChevronLeft className="h-4 w-4 mr-1" />
          Previous
        </Button>
        <span className="text-sm text-muted-foreground">
          Page {currentPage} of {totalPages || 1}
        </span>
        <Button variant="outline" size="sm" onClick={() => onPageChange(currentPage + 1)} disabled={!hasNextPage}>
          Next
          <ChevronRight className="h-4 w-4 ml-1" />
        </Button>
      </div>
    </div>
  );
}
