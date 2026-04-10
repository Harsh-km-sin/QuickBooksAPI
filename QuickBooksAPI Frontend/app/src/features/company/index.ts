/**
 * Company / realm / sync — QuickBooks linked companies and realm selection.
 * API: `/api/company/*` (see backend `CompanyController`).
 */
export { companyApi } from '@/api/companyApi';
export { setRealmId, getRealmId } from '@/api/core';
export { useConnectedCompanies } from './useConnectedCompanies';
export type { UseConnectedCompaniesOptions } from './useConnectedCompanies';
