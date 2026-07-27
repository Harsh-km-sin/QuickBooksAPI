import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { reportApi } from '@/api/reportApi';
import type { ReportTree, ReportPeriod, ReportType } from '@/types';
import { toast } from 'sonner';

const REPORTS_QUERY_KEY = ['reports'] as const;

interface UseReportReturn {
  report: ReportTree | null;
  isLoading: boolean;
  error: string | null;
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'An unexpected error occurred';
}

/**
 * Profit & Loss for a month-aligned range. Served entirely from our database — the backend snaps
 * partial months outward and sums the stored monthly columns.
 */
export function useProfitAndLoss(params: {
  startDate?: string;
  endDate?: string;
  useFiscalYear?: boolean;
  accountingMethod?: string;
  enabled?: boolean;
}): UseReportReturn {
  const { startDate, endDate, useFiscalYear = false, accountingMethod = 'Accrual', enabled = true } = params;

  const { data, isLoading, error } = useQuery({
    queryKey: [...REPORTS_QUERY_KEY, 'profit-and-loss', startDate ?? '', endDate ?? '', useFiscalYear, accountingMethod],
    queryFn: async () => {
      const response = await reportApi.profitAndLoss({ startDate, endDate, useFiscalYear, accountingMethod });
      if (!response.success || !response.data) {
        throw new Error(response.message || 'Failed to load the Profit and Loss report');
      }
      return response.data;
    },
    enabled: enabled && (useFiscalYear || (!!startDate && !!endDate)),
  });

  return { report: data ?? null, isLoading, error: error ? toErrorMessage(error) : null };
}

/** Balance Sheet as of a date. Point-in-time — the backend returns one month-end snapshot. */
export function useBalanceSheet(params: {
  asOfDate?: string;
  accountingMethod?: string;
  enabled?: boolean;
}): UseReportReturn {
  const { asOfDate, accountingMethod = 'Accrual', enabled = true } = params;

  const { data, isLoading, error } = useQuery({
    queryKey: [...REPORTS_QUERY_KEY, 'balance-sheet', asOfDate ?? '', accountingMethod],
    queryFn: async () => {
      const response = await reportApi.balanceSheet(asOfDate, accountingMethod);
      if (!response.success || !response.data) {
        throw new Error(response.message || 'Failed to load the Balance Sheet report');
      }
      return response.data;
    },
    enabled,
  });

  return { report: data ?? null, isLoading, error: error ? toErrorMessage(error) : null };
}

/** Which periods are resident locally, so the UI can show how much history exists. */
export function useReportPeriods(reportType: ReportType): {
  periods: ReportPeriod[];
  isLoading: boolean;
} {
  const { data, isLoading } = useQuery({
    queryKey: [...REPORTS_QUERY_KEY, 'periods', reportType],
    queryFn: async () => {
      const response = await reportApi.periods(reportType);
      if (!response.success || !response.data) {
        throw new Error(response.message || 'Failed to load synced report periods');
      }
      return response.data;
    },
  });

  return { periods: data ?? [], isLoading };
}

interface UseReportMutationsReturn {
  isGenerating: boolean;
  generateReports: () => Promise<void>;
}

/**
 * The only path that calls QuickBooks. Pulls all available history for both report types, so it
 * can take a while on a company with several years of books.
 */
export function useReportMutations(): UseReportMutationsReturn {
  const queryClient = useQueryClient();

  const syncMutation = useMutation({
    mutationFn: async () => {
      const response = await reportApi.sync();
      if (!response.success) {
        throw new Error(response.message || 'Failed to generate reports');
      }
      return response.data;
    },
    onSuccess: (periodCount) => {
      if (periodCount === 0) {
        toast.info('Reports are already up to date', {
          description: 'Nothing has changed in QuickBooks since the last pull.',
        });
      } else {
        toast.success('Reports generated', {
          description: `${periodCount} report period${periodCount === 1 ? '' : 's'} synced from QuickBooks`,
        });
      }
      // Both reports were re-pulled, so every cached range is stale.
      queryClient.invalidateQueries({ queryKey: REPORTS_QUERY_KEY });
    },
    onError: (err) => {
      toast.error('Failed to generate reports', { description: toErrorMessage(err) });
    },
  });

  return {
    isGenerating: syncMutation.isPending,
    generateReports: async () => {
      await syncMutation.mutateAsync().catch(() => undefined);
    },
  };
}
