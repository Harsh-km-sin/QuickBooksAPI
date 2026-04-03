export interface CashRunwayResult {
  currentCash: number;
  monthlyBurn: number;
  expectedRevenue: number;
  runwayMonths: number;
}

export interface VendorSpend {
  vendorName: string;
  totalSpend: number;
  billCount: number;
  lastBillDate: string | null;
  periodStart: string;
}

export interface VendorSpendSummary {
  totalSpend: number;
  vendorCount: number;
  billCount: number;
  from: string;
  to: string;
}

export interface CustomerProfitability {
  customerName: string;
  revenue: number;
  costOfGoods: number;
  grossMargin: number;
  marginPct: number;
  periodStart: string;
}

export interface RevenueExpensesMonthly {
  monthStart: string;
  revenue: number;
  expenses: number;
}

export interface Anomaly {
  id: number;
  type: string;
  severity: string;
  details: string | null;
  detectedAt: string;
}

export interface KpiSnapshot {
  snapshotDate: string;
  kpiName: string;
  kpiValue: number;
  period: string;
}

export interface ForecastScenario {
  id: number;
  name: string;
  createdAtUtc: string;
  createdBy: string | null;
  horizonMonths: number;
  assumptionsJson: string | null;
  status: string;
}

export interface ForecastResult {
  periodStart: string;
  revenue: number;
  expenses: number;
  netIncome: number;
  cashBalance: number;
  runwayMonths: number | null;
}

export interface ForecastDetail {
  scenario: ForecastScenario;
  results: ForecastResult[];
}

export interface CreateForecastRequest {
  name: string;
  horizonMonths?: number;
  assumptionsJson?: string | null;
}

export interface CloseIssue {
  id: number;
  issueType: string;
  severity: string;
  details: string | null;
  detectedAt: string;
  resolvedAt: string | null;
}

export interface Entity {
  id: number;
  name: string;
  realmId: string;
  isConsolidatedNode: boolean;
}

export interface ConsolidatedPnlRow {
  periodStart: string;
  periodEnd: string;
  revenue: number;
  expenses: number;
  netIncome: number;
}

export interface CfoAssistantCitation {
  metricName: string;
  dateRange?: string | null;
  endpoint?: string | null;
}

export interface CfoAssistantResponse {
  answer: string;
  citations: CfoAssistantCitation[];
}
