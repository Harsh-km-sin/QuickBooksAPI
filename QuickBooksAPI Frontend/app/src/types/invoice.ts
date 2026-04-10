/** Invoice list row from `InvoiceListItemDto` (aligned with backend read APIs). */
export interface QBOInvoiceHeader {
  invoiceId: number;
  qboInvoiceId: string;
  realmId: string;
  syncToken: string;
  domain: string | null;
  sparse: boolean;
  txnDate: string;
  dueDate: string;
  customerRefId: string | null;
  customerRefName: string | null;
  currencyCode: string | null;
  exchangeRate: number;
  totalAmt: number;
  homeTotalAmt?: number;
  balance: number;
  homeBalance?: number;
  globalTaxCalculation?: string | null;
  privateNote?: string | null;
  createTime: string;
  lastUpdatedTime: string;
  rawJson: string | null;
}

export interface CreateInvoiceLineRequest {
  detailType: 'SalesItemLineDetail';
  amount: number;
  description?: string;
  salesItemLineDetail: {
    itemRef: { value: string; name: string };
    qty: number;
    unitPrice: number;
    taxCodeRef?: { value: string; name: string };
  };
}

export interface CreateInvoiceRequest {
  line: CreateInvoiceLineRequest[];
  customerRef: { value: string; name: string };
  txnDate: string;
  dueDate?: string;
}

export interface UpdateInvoiceRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  customerRef?: { value: string; name: string };
  txnDate?: string;
  dueDate?: string;
  line?: CreateInvoiceLineRequest[];
}

export interface DeleteInvoiceRequest {
  id: string;
  syncToken: string;
}

export interface VoidInvoiceRequest {
  id: string;
  syncToken: string;
}
