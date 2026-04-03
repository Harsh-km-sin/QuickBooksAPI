export interface DashboardStats {
  customersCount: number;
  productsCount: number;
  vendorsCount: number;
  billsCount: number;
  invoicesCount: number;
  totalInvoiceAmount: number;
  totalBillAmount: number;
  outstandingInvoiceBalance: number;
  outstandingBillBalance: number;
}

export interface NavItem {
  title: string;
  href: string;
  icon: string;
}
