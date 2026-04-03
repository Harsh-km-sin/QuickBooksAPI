export interface ChartOfAccounts {
  id: number;
  qboId: string;
  name: string;
  subAccount: boolean;
  fullyQualifiedName: string;
  active: boolean;
  classification: string | null;
  accountType: string | null;
  accountSubType: string | null;
  currentBalance: number;
  currentBalanceWithSubAccounts: number;
  currencyRefValue: string | null;
  currencyRefName: string | null;
  domain: string | null;
  sparse: boolean;
  syncToken: string;
  createTime: string;
  lastUpdatedTime: string;
  userId: number;
  realmId: string;
}
