import { useState, useCallback } from 'react';
import { assistantApi } from '@/api/client';
import type { CfoAssistantResponse } from '@/types';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  citations?: CfoAssistantResponse['citations'];
}

export function useCfoAssistant() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const ask = useCallback(async (question: string) => {
    if (!question.trim()) return;
    setMessages((prev) => [...prev, { role: 'user', content: question.trim() }]);
    setIsLoading(true);
    setError(null);
    try {
      const res = await assistantApi.ask(question.trim());
      if (res.success && res.data) {
        setMessages((prev) => [
          ...prev,
          { role: 'assistant', content: res.data!.answer, citations: res.data!.citations },
        ]);
      } else {
        setMessages((prev) => [...prev, { role: 'assistant', content: res.message || 'No response.' }]);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Request failed';
      setError(message);
      setMessages((prev) => [...prev, { role: 'assistant', content: `Error: ${message}` }]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clear = useCallback(() => {
    setMessages([]);
    setError(null);
  }, []);

  return { messages, isLoading, error, ask, clear };
}
