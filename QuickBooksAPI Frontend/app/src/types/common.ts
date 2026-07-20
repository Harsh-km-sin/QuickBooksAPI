export type SortDirection = 'asc' | 'desc';

export interface ListQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  activeFilter?: 'active' | 'inactive' | 'all';
  sortBy?: string;
  sortDir?: SortDirection;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}
