import { useMemo, useState } from 'react';
import { useDebouncedValue } from './useDebouncedValue';
import type { DataTableSort } from '@/components/data-table';
import type { ListQueryParams, SortDirection } from '@/types';

const SEARCH_DEBOUNCE_MS = 300;

export interface UseListQueryStateOptions {
  defaultPageSize?: number;
  defaultSortBy?: string;
  defaultSortDir?: SortDirection;
  defaultActiveFilter?: 'active' | 'inactive' | 'all';
}

/**
 * Owns page/pageSize/search/sort/activeFilter state for a backend-paged list
 * and builds the `ListQueryParams` to fetch it. Meant to be called from a
 * self-contained table subcomponent (not the page) so that paging/sorting
 * only re-renders that subtree.
 */
export function useListQueryState(options?: UseListQueryStateOptions) {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(options?.defaultPageSize ?? 20);
  const [activeFilter, setActiveFilter] = useState<'active' | 'inactive' | 'all'>(options?.defaultActiveFilter ?? 'active');
  const [sortBy, setSortBy] = useState<string | undefined>(options?.defaultSortBy);
  const [sortDir, setSortDir] = useState<SortDirection>(options?.defaultSortDir ?? 'asc');
  const debouncedSearch = useDebouncedValue(searchTerm.trim(), SEARCH_DEBOUNCE_MS);

  const listParams = useMemo<ListQueryParams>(
    () => ({ page, pageSize, search: debouncedSearch || undefined, activeFilter, sortBy, sortDir }),
    [page, pageSize, debouncedSearch, activeFilter, sortBy, sortDir]
  );

  const goToPage = (nextPage: number) => setPage(Math.max(1, nextPage));

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    setPage(1);
  };

  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setPage(1);
  };

  const handleActiveFilterChange = (value: 'active' | 'inactive' | 'all') => {
    setActiveFilter(value);
    setPage(1);
  };

  const handleSortChange = (sort: DataTableSort) => {
    setSortBy(sort.sortBy);
    setSortDir(sort.sortDir);
    setPage(1);
  };

  return {
    searchTerm,
    debouncedSearch,
    activeFilter,
    sortBy,
    sortDir,
    listParams,
    goToPage,
    handleSearchChange,
    handlePageSizeChange,
    handleActiveFilterChange,
    handleSortChange,
  };
}
