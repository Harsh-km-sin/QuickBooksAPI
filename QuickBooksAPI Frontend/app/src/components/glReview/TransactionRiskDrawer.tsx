import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import { CheckCircle2, Sparkles } from 'lucide-react';
import { useGlTransaction, useMarkReviewed } from '@/hooks';
import { tierStyle, anomalyLabel } from './riskTier';
import type { GlAnomaly } from '@/types';

const formatCurrency = (v: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(v);

function MetadataList({ metadata }: { metadata: unknown }) {
  if (!metadata || typeof metadata !== 'object') return null;
  const entries = Object.entries(metadata as Record<string, unknown>).filter(
    ([, v]) => v != null && typeof v !== 'object',
  );
  const arrays = Object.entries(metadata as Record<string, unknown>).filter(([, v]) => Array.isArray(v));
  if (entries.length === 0 && arrays.length === 0) return null;
  return (
    <div className="mt-2 space-y-1">
      {arrays.map(([k, v]) => (
        <div key={k} className="flex flex-wrap items-center gap-1">
          <span className="text-xs text-muted-foreground">{k}:</span>
          {(v as unknown[]).map((item, i) => (
            <Badge key={i} variant="outline" className="text-[10px]">{String(item)}</Badge>
          ))}
        </div>
      ))}
      {entries.length > 0 && (
        <div className="grid grid-cols-2 gap-x-3 gap-y-0.5 text-xs">
          {entries.map(([k, v]) => (
            <div key={k} className="flex justify-between gap-2">
              <span className="text-muted-foreground">{k}</span>
              <span className="font-medium tabular-nums truncate">{String(v)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export interface TransactionRiskDrawerProps {
  runId: number;
  txId: number | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TransactionRiskDrawer({ runId, txId, open, onOpenChange }: TransactionRiskDrawerProps) {
  const { data: tx, isLoading } = useGlTransaction(runId, txId);
  const markReviewed = useMarkReviewed(runId);

  const tier = tierStyle(tx?.riskTier);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="flex items-center gap-2">
            Entry risk detail
            {tx && <Badge variant={tier.variant} className={tier.className}>{tier.label}</Badge>}
          </SheetTitle>
          <SheetDescription>Per-detector evidence for this journal entry.</SheetDescription>
        </SheetHeader>

        {isLoading || !tx ? (
          <div className="p-4 space-y-3">
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-40 w-full" />
          </div>
        ) : (
          <div className="px-4 pb-6 space-y-4">
            {/* Entry facts */}
            <div className="grid grid-cols-2 gap-2 text-sm">
              <Fact label="Account" value={tx.accountName} />
              <Fact label="Date" value={new Date(tx.transactionDate).toLocaleDateString()} />
              <Fact label="Amount" value={formatCurrency(Number(tx.amount))} />
              <Fact label="Posting" value={tx.postingType ?? '—'} />
              <Fact label="Entity" value={tx.entityName ?? '—'} />
              <Fact label="Risk score" value={tx.riskScore != null ? `${tx.riskScore}/100` : '—'} />
              {tx.journalEntryId && <Fact label="JE #" value={tx.journalEntryId} />}
              {tx.zScore != null && <Fact label="Z-score" value={tx.zScore.toFixed(2)} />}
            </div>
            {tx.description && <p className="text-sm text-muted-foreground">{tx.description}</p>}

            {tx.aiExplanation && (
              <div className="rounded-md bg-primary/5 border border-primary/15 p-3">
                <p className="flex items-center gap-1.5 text-xs font-medium text-primary mb-1">
                  <Sparkles className="h-3.5 w-3.5" /> AI assessment
                </p>
                <p className="text-sm">{tx.aiExplanation}</p>
              </div>
            )}

            <Separator />

            {/* Fired flags */}
            <div>
              <p className="text-sm font-medium mb-2">
                Fired detectors {tx.anomalyFlags?.length ? `(${tx.anomalyFlags.length})` : ''}
              </p>
              {tx.anomalyFlags?.length ? (
                <div className="flex flex-wrap gap-1.5">
                  {tx.anomalyFlags.map((f) => (
                    <Badge key={f} variant="secondary">{anomalyLabel(f)}</Badge>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">No anomalies fired for this entry.</p>
              )}
            </div>

            {/* Per-detector evidence */}
            {tx.anomalyDetails && tx.anomalyDetails.length > 0 && (
              <div className="space-y-3">
                <p className="text-sm font-medium">Evidence</p>
                {tx.anomalyDetails.map((a: GlAnomaly, i) => (
                  <div key={`${a.anomalyType}-${i}`} className="rounded-md border p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">{anomalyLabel(a.anomalyType)}</span>
                      <Badge variant="outline" className="tabular-nums">
                        {Math.round(a.detectorScore * 100)}%
                      </Badge>
                    </div>
                    {a.riskReasons?.map((r, j) => (
                      <p key={j} className="mt-1 text-sm text-muted-foreground">{r}</p>
                    ))}
                    <MetadataList metadata={a.metadata} />
                  </div>
                ))}
              </div>
            )}

            <Separator />

            {/* Review action */}
            <div className="flex items-center justify-between">
              {tx.isReviewed ? (
                <span className="flex items-center gap-1.5 text-sm text-success">
                  <CheckCircle2 className="h-4 w-4" /> Reviewed
                </span>
              ) : (
                <span className="text-sm text-muted-foreground">Not yet reviewed</span>
              )}
              <Button
                size="sm"
                disabled={tx.isReviewed || markReviewed.isPending}
                onClick={() => markReviewed.mutate({ txId: tx.id })}
              >
                <CheckCircle2 className="h-4 w-4 mr-2" /> Mark reviewed
              </Button>
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-medium truncate">{value}</p>
    </div>
  );
}
