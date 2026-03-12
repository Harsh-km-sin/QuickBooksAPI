import { useState, useCallback } from 'react';
import { analyticsApi } from '@/api/client';
import type { ForecastDetail, CreateForecastRequest } from '@/types';

export function useForecast() {
  const [detail, setDetail] = useState<ForecastDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createForecast = useCallback(async (request: CreateForecastRequest) => {
    try {
      setIsLoading(true);
      setError(null);
      const res = await analyticsApi.createForecast(request);
      if (res.success && res.data) {
        const getRes = await analyticsApi.getForecast(res.data.id);
        if (getRes.success && getRes.data) setDetail(getRes.data);
        return res.data.id;
      }
      return null;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create forecast';
      setError(message);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadForecast = useCallback(async (id: number) => {
    try {
      setIsLoading(true);
      setError(null);
      const res = await analyticsApi.getForecast(id);
      if (res.success && res.data) setDetail(res.data);
      else setDetail(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load forecast';
      setError(message);
      setDetail(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clearDetail = useCallback(() => {
    setDetail(null);
  }, []);

  return { detail, isLoading, error, createForecast, loadForecast, clearDetail };
}
