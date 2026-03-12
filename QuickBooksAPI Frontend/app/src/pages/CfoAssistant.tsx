import { useState, useRef, useEffect } from 'react';
import { useCfoAssistant, type ChatMessage } from '@/hooks/useCfoAssistant';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { MessageCircle, Send } from 'lucide-react';

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';
  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[85%] rounded-lg px-3 py-2 ${
          isUser ? 'bg-primary text-primary-foreground' : 'bg-muted'
        }`}
      >
        <p className="text-sm whitespace-pre-wrap">{message.content}</p>
        {message.citations && message.citations.length > 0 && (
          <div className="mt-2 pt-2 border-t border-border/50 flex flex-wrap gap-1">
            {message.citations.map((c, i) => (
              <Badge key={i} variant="secondary" className="text-xs font-normal">
                {c.metricName}
                {c.dateRange ? ` (${c.dateRange})` : ''}
              </Badge>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export function CfoAssistant() {
  const { messages, isLoading, error, ask, clear } = useCfoAssistant();
  const [input, setInput] = useState('');
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isLoading) return;
    ask(input);
    setInput('');
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">CFO Assistant</h1>
        <p className="text-muted-foreground">
          Ask questions about runway, revenue vs expenses, top vendors, and customer profitability
        </p>
      </div>

      <Card className="flex flex-col h-[calc(100vh-12rem)] max-h-[600px]">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div>
            <CardTitle className="flex items-center gap-2">
              <MessageCircle className="h-5 w-5" />
              Chat
            </CardTitle>
            <CardDescription>Answers are grounded in your warehouse data</CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={clear}>
            Clear
          </Button>
        </CardHeader>
        <CardContent className="flex-1 flex flex-col min-h-0 p-0">
          <ScrollArea className="flex-1 px-4 pb-2">
            <div className="space-y-3 py-2">
              {messages.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-6">
                  Try: &quot;How many months of runway do we have?&quot; or &quot;Top unprofitable customers?&quot;
                </p>
              )}
              {messages.map((m, i) => (
                <MessageBubble key={i} message={m} />
              ))}
              {isLoading && (
                <div className="flex justify-start">
                  <div className="bg-muted rounded-lg px-3 py-2 text-sm text-muted-foreground">
                    Thinking...
                  </div>
                </div>
              )}
              <div ref={scrollRef} />
            </div>
          </ScrollArea>
          {error && (
            <p className="text-destructive text-sm px-4 pb-1">{error}</p>
          )}
          <form onSubmit={handleSubmit} className="flex gap-2 p-4 border-t">
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Ask a question..."
              disabled={isLoading}
              className="flex-1"
            />
            <Button type="submit" disabled={isLoading || !input.trim()}>
              <Send className="h-4 w-4" />
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
