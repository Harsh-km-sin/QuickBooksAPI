import { useState, useEffect, useCallback } from 'react';
import { analyticsApi } from '@/api/client';
import type { CloseIssue } from '@/types';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import { ClipboardCheck } from 'lucide-react';
import { toast } from 'sonner';

export function CloseAssistant() {
  const [issues, setIssues] = useState<CloseIssue[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [since, setSince] = useState<string>('');
  const [severity, setSeverity] = useState<string>('');
  const [unresolvedOnly, setUnresolvedOnly] = useState(true);
  const [resolvingId, setResolvingId] = useState<number | null>(null);

  const fetchIssues = useCallback(async () => {
    setLoading(true);
    try {
      const res = await analyticsApi.getCloseIssues({
        since: since || undefined,
        severity: severity || undefined,
        unresolvedOnly,
      });
      if (res.success && res.data) setIssues(res.data);
      else setIssues([]);
    } catch {
      setIssues([]);
      toast.error('Failed to load close issues.');
    } finally {
      setLoading(false);
    }
  }, [since, severity, unresolvedOnly]);

  useEffect(() => {
    fetchIssues();
  }, [fetchIssues]);

  const handleResolve = async (id: number) => {
    setResolvingId(id);
    try {
      const res = await analyticsApi.resolveCloseIssue(id);
      if (res.success) {
        toast.success('Issue marked as resolved.');
        await fetchIssues();
      } else {
        toast.error(res.message ?? 'Failed to resolve.');
      }
    } catch {
      toast.error('Failed to resolve issue.');
    } finally {
      setResolvingId(null);
    }
  };

  const formatDate = (s: string) => new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <ClipboardCheck className="h-8 w-8" />
          Close & Data Quality
        </h1>
        <p className="text-muted-foreground">Month-end close blockers and data-quality issues detected by the nightly job.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
          <CardDescription>Filter by detection date, severity, and resolved status.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4 items-end">
          <div className="space-y-2">
            <Label>Detected since</Label>
            <Select value={since} onValueChange={setSince}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Any" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Any</SelectItem>
                <SelectItem value={new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)}>Last 7 days</SelectItem>
                <SelectItem value={new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)}>Last 30 days</SelectItem>
                <SelectItem value={new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)}>Last 90 days</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Severity</Label>
            <Select value={severity} onValueChange={setSeverity}>
              <SelectTrigger className="w-[120px]">
                <SelectValue placeholder="Any" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Any</SelectItem>
                <SelectItem value="High">High</SelectItem>
                <SelectItem value="Medium">Medium</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox id="unresolved" checked={unresolvedOnly} onCheckedChange={(v) => setUnresolvedOnly(v === true)} />
            <Label htmlFor="unresolved">Unresolved only</Label>
          </div>
          <Button variant="secondary" onClick={fetchIssues}>Apply</Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Issues</CardTitle>
          <CardDescription>List of detected close and data-quality issues.</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-2">
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} className="h-16 w-full" />
              ))}
            </div>
          ) : !issues?.length ? (
            <p className="text-muted-foreground py-8 text-center">No issues match the current filters.</p>
          ) : (
            <ul className="space-y-3">
              {issues.map((i) => (
                <li key={i.id} className="flex flex-wrap items-center gap-3 rounded-lg border p-4">
                  <Badge variant={i.severity === 'High' ? 'destructive' : 'default'}>{i.severity}</Badge>
                  <span className="font-medium text-muted-foreground">{i.issueType}</span>
                  <span className="flex-1 min-w-0 text-sm">{i.details ?? '—'}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(i.detectedAt)}</span>
                  {!i.resolvedAt && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleResolve(i.id)}
                      disabled={resolvingId === i.id}
                    >
                      {resolvingId === i.id ? 'Resolving…' : 'Resolve'}
                    </Button>
                  )}
                  {i.resolvedAt && (
                    <Badge variant="secondary">Resolved {formatDate(i.resolvedAt)}</Badge>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
