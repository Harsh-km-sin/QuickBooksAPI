export interface QBOBillHeader {
  billId: number;
  qboBillId: string;
  realmId: string;
  syncToken: string;
  domain: string | null;
  sparse: boolean;
  apAccountRefValue: string | null;
  apAccountRefName: string | null;
  vendorRefValue: string | null;
  vendorRefName: string | null;
  txnDate: string | null;
  dueDate: string | null;
  totalAmt: number;
  balance: number;
  isDeleted: boolean;
  currencyRefValue: string | null;
  currencyRefName: string | null;
  salesTermRefValue: string | null;
  createTime: string;
  lastUpdatedTime: string;
  rawJson: string | null;
}

export interface CreateBillLineRequest {
  detailType: 'AccountBasedExpenseLineDetail' | 'ItemBasedExpenseLineDetail';
  amount: number;
  description?: string;
  accountBasedExpenseLineDetail?: {
    accountRef: { value: string; name: string };
    taxCodeRef?: { value: string; name: string };
    billableStatus?: string;
    customerRef?: { value: string; name: string };
  };
  itemBasedExpenseLineDetail?: {
    itemRef: { value: string; name: string };
    qty: number;
    unitPrice: number;
    taxCodeRef?: { value: string; name: string };
    billableStatus?: string;
  };
  projectRef?: { value: string; name: string };
}

export interface CreateBillRequest {
  line: CreateBillLineRequest[];
  vendorRef: { value: string; name: string };
  txnDate: string;
  dueDate?: string;
  docNumber?: string;
  apAccountRef?: { value: string; name: string };
  currencyRef?: { value: string; name: string };
  privateNote?: string;
  salesTermRef?: { value: string; name: string };
  departmentRef?: { value: string; name: string };
}

export interface UpdateBillRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  vendorRef?: { value: string; name: string };
  txnDate?: string;
  dueDate?: string;
  line?: CreateBillLineRequest[];
}

export interface DeleteBillRequest {
  id: string;
  syncToken: string;
}
