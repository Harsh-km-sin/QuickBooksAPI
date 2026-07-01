/**
 * GL Review types — mirror the .NET DTOs in
 * QuickBooksAPI/API/DTOs/Response/{GlRunDto,GlTransactionDto,GlAnalyticsDto}.cs
 * (System.Text.Json serialises camelCase).
 */

export type GlRunStatus = 'Pending' | 'Processing' | 'Complete' | 'Failed' | string;

export interface GlRun {
  id: number;
  fileName: string;
  fileType: string | null;
  fileSizeBytes: number | null;
  status: GlRunStatus;
  progressPercentage: number;
  sourceFormat: string | null;
  totalTransactions: number | null;
  flaggedCount: number | null;
  criticalCount: number | null;
  highCount: number | null;
  mediumCount: number | null;
  lowCount: number | null;
  normalCount: number | null;
  avgRiskScore: number | null;
  materialExposure: number | null;
  periodStart: string | null;
  periodEnd: string | null;
  aiExecutiveSummary: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  errorMessage: string | null;
}

export interface GlAnomaly {
  anomalyType: string;
  detectorScore: number;
  riskReasons: string[];
  metadata: unknown;
}

export interface GlFeedback {
  id?: number;
  transactionId?: number;
  isConfirmed?: boolean | null;
  note?: string | null;
}

export interface GlTransaction {
  id: number;
  runId: number;
  transactionDate: string;
  accountId: string | null;
  accountName: string;
  accountType: string | null;
  postingType: string | null;
  amount: number;
  entityName: string | null;
  description: string | null;
  sourceType: string | null;
  createdBy: string | null;
  journalEntryId: string | null;
  riskScore: number | null;
  compositeRiskScore: number | null;
  riskTier: string | null;
  anomalyFlags: string[];
  zScore: number | null;
  aiExplanation: string | null;
  riskExplanation: unknown;
  status: string;
  isReviewed: boolean;
  reviewedAt: string | null;
  reviewNote: string | null;
  anomalyDetails: GlAnomaly[] | null;
  feedback: GlFeedback | null;
}

export interface GlRunSummary {
  run: GlRun;
  riskDistribution: Record<string, number>;
  topFlagged: GlTransaction[];
}

export interface GlEntityRisk {
  party: string | null;
  maxRiskScore: number;
  flaggedCount: number;
  totalAmount: number;
}

export interface GlPeriodMetric {
  month: string;
  totalAmount: number;
  flaggedAmount: number;
  flaggedCount: number;
  avgRiskScore: number;
}

export interface GlAnomalyBreakdown {
  anomalyType: string;
  count: number;
  percentage: number;
}

/** Server returns the minimal paged shape (no computed flags). */
export interface GlPagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GlTransactionFilter {
  page?: number;
  pageSize?: number;
  riskTier?: string;
  accountName?: string;
  entityName?: string;
  dateFrom?: string;
  dateTo?: string;
  amountMin?: number;
  amountMax?: number;
  unreviewedOnly?: boolean;
}
