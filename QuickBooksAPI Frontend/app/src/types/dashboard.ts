export interface DashboardStats {
  totalIncome: number | null;
  totalExpenses: number | null;
  netIncome: number | null;
  pnlRangeStart: string | null;
  pnlRangeEnd: string | null;
  pnlAccountingMethod: string | null;

  totalAssets: number | null;
  totalLiabilities: number | null;
  totalEquity: number | null;
  bsAsOfDate: string | null;
  bsAccountingMethod: string | null;

  customersCount: number;
  vendorsCount: number;
  productsCount: number;
  invoicesCount: number;
  billsCount: number;
}

export interface NavItem {
  title: string;
  href: string;
  icon: string;
}
