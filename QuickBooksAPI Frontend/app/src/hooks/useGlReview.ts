/**
 * GL Review data hooks (react-query). The QueryClientProvider is already mounted in main.tsx.
 * Selected-run state lives in the `glReview` Redux slice so it is shared across pages.
 */
import { useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { glReviewApi } from '@/api/glReviewApi';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { setSelectedRun } from '@/store/slices/glReviewSlice';
import type { GlRun, GlTransactionFilter } from '@/types';

const KEY = {
  runs: ['gl', 'runs'] as const,
  run: (id: number) => ['gl', 'run', id] as const,
  summary: (id: number) => ['gl', 'run', id, 'summary'] as const,
  transactions: (id: number, f?: GlTransactionFilter) => ['gl', 'run', id, 'transactions', f ?? {}] as const,
  transaction: (id: number, txId: number) => ['gl', 'run', id, 'tx', txId] as const,
  entityRisk: (id: number) => ['gl', 'run', id, 'entity-risk'] as const,
  periodMetrics: (id: number) => ['gl', 'run', id, 'period-metrics'] as const,
  anomalyBreakdown: (id: number) => ['gl', 'run', id, 'anomaly-breakdown'] as const,
};

const TERMINAL = new Set(['Complete', 'Failed']);
const isInFlight = (r: GlRun) => !TERMINAL.has(r.status);

/** All runs for the user, sorted newest first. Polls while any run is still processing. */
export function useGlRuns() {
  return useQuery({
    queryKey: KEY.runs,
    queryFn: async () => {
      const res = await glReviewApi.listRuns();
      const runs = res.data ?? [];
      return [...runs].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    },
    refetchInterval: (query) => (query.state.data?.some(isInFlight) ? 3000 : false),
  });
}

/**
 * Resolves the run to display: the user's choice from the slice, or the most recent
 * completed run as a default. Also exposes the runs list and a setter.
 */
export function useSelectedRun() {
  const dispatch = useAppDispatch();
  const chosenId = useAppSelector((s) => s.glReview.selectedRunId);
  const { data: runs, isLoading } = useGlRuns();

  const selectedRun = useMemo(() => {
    if (!runs?.length) return null;
    if (chosenId != null) {
      const match = runs.find((r) => r.id === chosenId);
      if (match) return match;
    }
    return runs.find((r) => r.status === 'Complete') ?? runs[0];
  }, [runs, chosenId]);

  return {
    runs: runs ?? [],
    isLoading,
    selectedRun,
    selectedRunId: selectedRun?.id ?? null,
    selectRun: (id: number | null) => dispatch(setSelectedRun(id)),
  };
}

export function useGlRunSummary(runId: number | null | undefined) {
  return useQuery({
    queryKey: KEY.summary(runId ?? 0),
    queryFn: async () => (await glReviewApi.getSummary(runId!)).data,
    enabled: runId != null,
  });
}

export function useGlTransactions(runId: number | null | undefined, filter?: GlTransactionFilter) {
  return useQuery({
    queryKey: KEY.transactions(runId ?? 0, filter),
    queryFn: async () => (await glReviewApi.listTransactions(runId!, filter)).data,
    enabled: runId != null,
    placeholderData: (prev) => prev,
  });
}

export function useGlTransaction(runId: number | null | undefined, txId: number | null | undefined) {
  return useQuery({
    queryKey: KEY.transaction(runId ?? 0, txId ?? 0),
    queryFn: async () => (await glReviewApi.getTransaction(runId!, txId!)).data,
    enabled: runId != null && txId != null,
  });
}

export function useGlEntityRisk(runId: number | null | undefined) {
  return useQuery({
    queryKey: KEY.entityRisk(runId ?? 0),
    queryFn: async () => (await glReviewApi.getEntityRisk(runId!)).data ?? [],
    enabled: runId != null,
  });
}

export function useGlPeriodMetrics(runId: number | null | undefined) {
  return useQuery({
    queryKey: KEY.periodMetrics(runId ?? 0),
    queryFn: async () => (await glReviewApi.getPeriodMetrics(runId!)).data ?? [],
    enabled: runId != null,
  });
}

export function useGlAnomalyBreakdown(runId: number | null | undefined) {
  return useQuery({
    queryKey: KEY.anomalyBreakdown(runId ?? 0),
    queryFn: async () => (await glReviewApi.getAnomalyBreakdown(runId!)).data ?? [],
    enabled: runId != null,
  });
}

/** Upload a GL file, then select the new run. The runs query polls until it completes. */
export function useUploadGlFile() {
  const qc = useQueryClient();
  const dispatch = useAppDispatch();
  return useMutation({
    mutationFn: async (file: File) => {
      const res = await glReviewApi.uploadFile(file);
      return res.data;
    },
    onSuccess: (data) => {
      if (data?.runId != null) dispatch(setSelectedRun(data.runId));
      qc.invalidateQueries({ queryKey: KEY.runs });
      toast.success('File uploaded — analysis started.');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Upload failed.'),
  });
}

export function useMarkReviewed(runId: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ txId, note }: { txId: number; note?: string }) => glReviewApi.markReviewed(runId, txId, note),
    onSuccess: (_res, { txId }) => {
      qc.invalidateQueries({ queryKey: KEY.transaction(runId, txId) });
      qc.invalidateQueries({ queryKey: ['gl', 'run', runId, 'transactions'] });
      toast.success('Marked as reviewed.');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Could not update review status.'),
  });
}

export function useRetryRun() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (runId: number) => glReviewApi.retryRun(runId),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY.runs }),
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Retry failed.'),
  });
}

export function useDeleteRun() {
  const qc = useQueryClient();
  const dispatch = useAppDispatch();
  return useMutation({
    mutationFn: (runId: number) => glReviewApi.deleteRun(runId),
    onSuccess: () => {
      dispatch(setSelectedRun(null));
      qc.invalidateQueries({ queryKey: KEY.runs });
      toast.success('Run deleted.');
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Delete failed.'),
  });
}
