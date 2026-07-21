import type { ListQueryParams } from '@/types';

/** Builds `?page=&pageSize=&search=&activeFilter=&sortBy=&sortDir=` for list endpoints. */
export function buildListQuery(params?: ListQueryParams): string {
  if (!params) return '';
  const search = new URLSearchParams();
  if (params.page != null) search.set('page', String(params.page));
  if (params.pageSize != null) search.set('pageSize', String(params.pageSize));
  if (params.search) search.set('search', params.search);
  if (params.activeFilter && params.activeFilter !== 'all') search.set('activeFilter', params.activeFilter);
  if (params.sortBy) search.set('sortBy', params.sortBy);
  if (params.sortDir) search.set('sortDir', params.sortDir);
  const q = search.toString();
  return q ? `?${q}` : '';
}
