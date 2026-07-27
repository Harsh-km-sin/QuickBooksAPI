import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { termApi } from '@/api/termApi';

export const TERMS_QUERY_KEY = ['terms'];

export function useTermsList(activeOnly: boolean = true) {
  return useQuery({
    queryKey: [...TERMS_QUERY_KEY, activeOnly],
    queryFn: async () => {
      let res = await termApi.list(activeOnly);
      let data = res.data ?? [];
      
      // If terms table is empty in DB, auto-trigger a sync from QBO once
      if (data.length === 0) {
        try {
          await termApi.sync();
          res = await termApi.list(activeOnly);
          data = res.data ?? [];
        } catch {
          // Ignore auto-sync errors if user is not connected or token expired
        }
      }

      return data;
    },
    staleTime: 1000 * 60 * 15, // 15 minutes
  });
}

export function useSyncTerms() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => termApi.sync(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: TERMS_QUERY_KEY });
    },
  });
}
