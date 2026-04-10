import { useState, useEffect, useCallback } from 'react';
import { toast } from 'sonner';
import { companyApi } from '@/api/companyApi';
import type { ConnectedCompany } from '@/types';

export type UseConnectedCompaniesOptions = {
  /** When true, fetch errors do not toast (e.g. sidebar). Default false. */
  silent?: boolean;
};

/**
 * Loads QuickBooks companies linked to the current user (GET `/api/company/connected-companies`).
 */
export function useConnectedCompanies(options?: UseConnectedCompaniesOptions) {
  const silent = options?.silent ?? false;
  const [companies, setCompanies] = useState<ConnectedCompany[]>([]);
  const [loading, setLoading] = useState(true);

  const refetch = useCallback(async () => {
    setLoading(true);
    try {
      const response = await companyApi.getConnectedCompanies();
      if (response.success && response.data) {
        setCompanies(response.data);
      } else {
        setCompanies([]);
      }
    } catch {
      setCompanies([]);
      if (!silent) {
        toast.error('Failed to load connected companies');
      }
    } finally {
      setLoading(false);
    }
  }, [silent]);

  useEffect(() => {
    void refetch();
  }, [refetch]);

  return { companies, loading, refetch };
}
