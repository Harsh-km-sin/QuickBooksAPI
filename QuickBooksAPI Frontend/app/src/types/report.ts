/** Mirrors QuickBooksAPI/Application/Dtos/ReportDtos.cs */

export type ReportType = 'ProfitAndLoss' | 'BalanceSheet';

/** A node in the report tree. Amounts arrive already aggregated by SQL — never recompute them here. */
export interface ReportNode {
  rowPath: string;
  label: string | null;
  /** 'Data' = a single account's postings, 'Section' = a computed subtotal node. */
  rowType: string;
  /** 'Income', 'Expenses', etc. Present on top-level sections only. */
  groupName: string | null;
  accountQboId: string | null;
  depth: number;
  isSummary: boolean;
  amount: number | null;
  children: ReportNode[];
}

export interface ReportTree {
  reportType: ReportType;
  accountingMethod: string;
  rangeStart: string;
  rangeEnd: string;
  /**
   * True for the Balance Sheet, where the report is a snapshot at `rangeEnd` and `rangeStart`
   * carries no meaning. Balance sheet figures are never summed across months.
   */
  isPointInTime: boolean;
  rows: ReportNode[];
}

/** One period resident in our database — used to bound the date pickers. */
export interface ReportPeriod {
  periodStart: string;
  periodEnd: string;
  accountingMethod: string;
  granularity: string;
  noReportData: boolean;
  syncedAtUtc: string;
}
