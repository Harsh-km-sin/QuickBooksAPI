import { Link } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
} from 'recharts';
import { ClipboardCheck, AlertTriangle } from 'lucide-react';
import type { Anomaly, CloseIssue } from '@/types';

export interface DashboardInsightsProps {
  chartColors: string[];
  closeIssues: CloseIssue[] | null;
  anomalies: Anomaly[] | undefined;
  kpiSparklineData: { date: string; GrossMargin?: number; RevenueGrowth?: number; BurnMultiple?: number }[];
  kpiSparklineSeries: string[];
  formatDate: (s: string) => string;
}

export function DashboardInsights({
  chartColors,
  closeIssues,
  anomalies,
  kpiSparklineData,
  kpiSparklineSeries,
  formatDate,
}: DashboardInsightsProps) {
  return (
    <>
      {closeIssues != null && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-2">
                <ClipboardCheck className="h-5 w-5 text-primary" />
                Close & Data Quality
              </span>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/close-assistant">View all</Link>
              </Button>
            </CardTitle>
            <CardDescription>Month-end close blockers and data-quality issues</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-2 text-2xl font-bold mb-2">
              <span className={closeIssues.length > 0 ? 'text-destructive' : 'text-success'}>
                {closeIssues.filter((i) => i.severity === 'High').length} High
              </span>
              <span className="text-muted-foreground">/</span>
              <span>{closeIssues.filter((i) => i.severity === 'Medium').length} Medium</span>
            </div>
            <ul className="space-y-2 max-h-48 overflow-y-auto">
              {closeIssues.slice(0, 5).map((i) => (
                <li key={i.id} className="flex flex-wrap items-start gap-2 rounded border p-2 text-sm">
                  <Badge variant={i.severity === 'High' ? 'destructive' : 'default'}>{i.severity}</Badge>
                  <span className="font-medium text-muted-foreground">{i.issueType}</span>
                  <span className="flex-1 min-w-0">{i.details ?? '—'}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(i.detectedAt)}</span>
                </li>
              ))}
              {closeIssues.length === 0 && (
                <li className="text-sm text-muted-foreground py-2">No open issues.</li>
              )}
            </ul>
          </CardContent>
        </Card>
      )}

      {anomalies && anomalies.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              Anomalies
            </CardTitle>
            <CardDescription>Recent flags from anomaly detection (e.g. spend spikes, large transactions, overdue receivables)</CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2 max-h-48 overflow-y-auto">
              {anomalies.slice(0, 10).map((a) => (
                <li key={a.id} className="flex flex-wrap items-start gap-2 rounded border p-2 text-sm">
                  <Badge variant={a.severity === 'High' ? 'destructive' : a.severity === 'Medium' ? 'default' : 'secondary'}>
                    {a.severity}
                  </Badge>
                  <span className="font-medium text-muted-foreground">{a.type}</span>
                  <span className="flex-1 min-w-0">{a.details ?? '—'}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(a.detectedAt)}</span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {kpiSparklineData.length > 0 && kpiSparklineSeries.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>KPI trends</CardTitle>
            <CardDescription>Last 6 months (Gross Margin %, Revenue Growth %, Burn Multiple)</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              {kpiSparklineSeries.map((name, i) => (
                <div key={name} className="h-[120px]">
                  <p className="text-xs font-medium text-muted-foreground mb-1">
                    {name === 'GrossMargin' ? 'Gross Margin %' : name === 'RevenueGrowth' ? 'Revenue Growth %' : 'Burn Multiple'}
                  </p>
                  <ResponsiveContainer width="100%" height={100}>
                    <LineChart data={kpiSparklineData}>
                      <XAxis dataKey="date" hide />
                      <YAxis width={28} tick={{ fontSize: 10 }} tickFormatter={(v) => (name === 'BurnMultiple' ? v : `${v}%`)} />
                      <Tooltip
                        formatter={(v: number) => (name === 'BurnMultiple' ? v.toFixed(2) : `${Number(v).toFixed(1)}%`)}
                        labelFormatter={(l) => formatDate(l)}
                      />
                      <Line type="monotone" dataKey={name} stroke={chartColors[i]} dot={false} strokeWidth={2} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </>
  );
}
