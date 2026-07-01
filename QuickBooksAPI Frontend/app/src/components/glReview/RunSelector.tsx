import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { useSelectedRun } from '@/hooks';

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'success' | 'outline' {
  if (status === 'Complete') return 'success';
  if (status === 'Failed') return 'destructive';
  if (status === 'Processing' || status === 'Pending') return 'secondary';
  return 'outline';
}

export function RunSelector({ className }: { className?: string }) {
  const { runs, selectedRunId, selectRun, isLoading } = useSelectedRun();

  if (!isLoading && runs.length === 0) {
    return <span className={`text-sm text-muted-foreground ${className ?? ''}`}>No runs yet</span>;
  }

  return (
    <Select
      value={selectedRunId != null ? String(selectedRunId) : undefined}
      onValueChange={(v) => selectRun(Number(v))}
      disabled={isLoading || runs.length === 0}
    >
      <SelectTrigger className={`w-[280px] ${className ?? ''}`}>
        <SelectValue placeholder={isLoading ? 'Loading runs…' : 'Select a run'} />
      </SelectTrigger>
      <SelectContent>
        {runs.map((run) => (
          <SelectItem key={run.id} value={String(run.id)}>
            <div className="flex items-center gap-2">
              <span className="max-w-[150px] truncate font-medium">{run.fileName}</span>
              <span className="text-xs text-muted-foreground">
                {new Date(run.createdAt).toLocaleDateString()}
              </span>
              <Badge variant={statusVariant(run.status)} className="ml-auto text-[10px]">
                {run.status === 'Processing' ? `${run.progressPercentage}%` : run.status}
              </Badge>
            </div>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
