import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TrendingUp, TrendingDown } from 'lucide-react';

export interface DashboardStatCardProps {
  title: string;
  value: string | number;
  description?: string;
  icon: React.ElementType;
  trend?: string;
  trendUp?: boolean;
  color?: string;
}

export function DashboardStatCard({
  title,
  value,
  description,
  icon: Icon,
  trend,
  trendUp,
  color = 'primary',
}: DashboardStatCardProps) {
  const topBarColor = color === 'success' ? 'bg-success' : color === 'destructive' ? 'bg-destructive' : 'bg-primary';
  const iconBgColor = color === 'success' ? 'bg-success/10' : color === 'destructive' ? 'bg-destructive/10' : 'bg-primary/10';
  const iconTextColor = color === 'success' ? 'text-success' : color === 'destructive' ? 'text-destructive' : 'text-primary';

  return (
    <Card className="overflow-hidden">
      <div className={`h-1 w-full ${topBarColor}`} />
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <div className={`${iconBgColor} p-2 rounded-md`}>
          <Icon className={`h-4 w-4 ${iconTextColor}`} />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
        {trend && (
          <div className={`flex items-center text-xs mt-1 ${trendUp ? 'text-success' : 'text-destructive'}`}>
            {trendUp ? <TrendingUp className="h-3 w-3 mr-1" /> : <TrendingDown className="h-3 w-3 mr-1" />}
            {trend}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
