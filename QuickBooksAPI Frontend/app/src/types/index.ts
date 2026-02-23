// List query params (pagination and search)
export interface ListQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  activeFilter?: 'active' | 'inactive' | 'all';
}

// Paged result from list endpoints
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// API Response wrapper
export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}

// JWT Token claims
export interface JwtClaims {
  UserId: string;
  NameIdentifier: string;
  Name: string;
  RealmIds: string[];
}

// User types
export interface UserSignUpRequest {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  password: string;
}

export interface UserLoginRequest {
  email: string;
  password: string;
}

// Connected QuickBooks company (from GET /api/auth/connected-companies)
export interface ConnectedCompany {
  id: number;
  qboRealmId: string;
  companyName: string | null;
  connectedAtUtc: string | null;
  isQboConnected: boolean;
}

// Customer Entity
export interface Customer {
  id: number;
  qboId: string;
  userId: string;
  realmId: string;
  syncToken: string;
  title: string;
  givenName: string;
  middleName: string;
  familyName: string;
  displayName: string;
  companyName: string;
  active: boolean;
  balance: number;
  primaryEmailAddr: string;
  primaryPhone: string;
  billAddrLine1: string;
  billAddrCity: string;
  billAddrPostalCode: string;
  billAddrCountrySubDivisionCode: string;
  createTime: string;
  lastUpdatedTime: string;
  domain: string;
  sparse: boolean;
}

export interface CreateCustomerRequest {
  givenName: string;
  middleName?: string;
  familyName: string;
  title?: string;
  suffix?: string;
  displayName: string;
  fullyQualifiedName?: string;
  companyName?: string;
  notes?: string;
  primaryEmailAddr?: { address: string };
  primaryPhone?: { freeFormNumber: string };
  billAddr?: {
    line1: string;
    city: string;
    countrySubDivisionCode: string;
    postalCode: string;
    country?: string;
  };
}

export interface UpdateCustomerRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  displayName?: string;
  givenName?: string;
  familyName?: string;
  companyName?: string;
  primaryEmailAddr?: { address: string };
  primaryPhone?: { freeFormNumber: string };
  billAddr?: {
    line1: string;
    city: string;
    countrySubDivisionCode: string;
    postalCode: string;
    country?: string;
  };
  active?: boolean;
  balance?: number;
}

export interface DeleteCustomerRequest {
  id: string;
  syncToken: string;
  sparse: boolean;
  active: boolean;
}

// Products Entity
export interface Products {
  id: number;
  qboId: string;
  name: string;
  description: string | null;
  active: boolean;
  fullyQualifiedName: string;
  taxable: boolean;
  unitPrice: number;
  type: string;
  qtyOnHand: number | null;
  incomeAccountRefValue: string | null;
  incomeAccountRefName: string | null;
  expenseAccountRefValue: string | null;
  expenseAccountRefName: string | null;
  assetAccountRefValue: string | null;
  assetAccountRefName: string | null;
  purchaseCost: number;
  trackQtyOnHand: boolean;
  invStartDate: string | null;
  domain: string;
  sparse: boolean;
  syncToken: string;
  createTime: string;
  lastUpdatedTime: string;
  userId: number;
  realmId: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  active?: boolean;
  trackQtyOnHand?: boolean;
  type: string;
  incomeAccountRef?: { name: string; value: string };
  expenseAccountRef?: { name: string; value: string };
  assetAccountRef?: { name: string; value: string };
  unitPrice?: number;
  purchaseCost?: number;
  qtyOnHand?: number;
  invStartDate?: string;
}

export interface UpdateProductRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  name?: string;
  type?: string;
  unitPrice?: number;
  purchaseCost?: number;
  qtyOnHand?: number;
  invStartDate?: string;
  incomeAccountRef?: { name: string; value: string };
  expenseAccountRef?: { name: string; value: string };
  assetAccountRef?: { name: string; value: string };
}

export interface DeleteProductRequest {
  id: string;
  syncToken: string;
  sparse: boolean;
  active: boolean;
}

// Vendor Entity
export interface Vendor {
  id: number;
  qboId: string;
  userId: string;
  realmId: string;
  syncToken: string;
  title: string;
  givenName: string;
  middleName: string;
  familyName: string;
  suffix: string;
  displayName: string;
  companyName: string;
  printOnCheckName: string;
  active: boolean;
  balance: number;
  primaryEmailAddr: string;
  primaryPhone: string;
  mobile: string;
  webAddr: string;
  taxIdentifier: string;
  acctNum: string;
  billAddrLine1: string;
  billAddrLine2: string;
  billAddrLine3: string;
  billAddrCity: string;
  billAddrPostalCode: string;
  billAddrCountrySubDivisionCode: string;
  billAddrCountry: string;
  domain: string;
  sparse: boolean;
  createTime: string;
  lastUpdatedTime: string;
  deletedAt: string | null;
  deletedBy: string | null;
}

export interface CreateVendorRequest {
  displayName: string;
  givenName?: string;
  middleName?: string;
  familyName?: string;
  companyName?: string;
  title?: string;
  suffix?: string;
  primaryEmailAddr?: { address: string };
  primaryPhone?: { freeFormNumber: string };
  mobile?: { freeFormNumber: string };
  webAddr?: { uri: string };
  billAddr?: {
    line1: string;
    line2?: string;
    line3?: string;
    city: string;
    postalCode: string;
    countrySubDivisionCode: string;
    country?: string;
  };
  printOnCheckName?: string;
  acctNum?: string;
  taxIdentifier?: string;
}

export interface UpdateVendorRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  displayName?: string;
  givenName?: string;
  middleName?: string;
  familyName?: string;
  companyName?: string;
  title?: string;
  suffix?: string;
  primaryEmailAddr?: { address: string };
  primaryPhone?: { freeFormNumber: string };
  mobile?: { freeFormNumber: string };
  webAddr?: { uri: string };
  billAddr?: {
    line1: string;
    line2?: string;
    line3?: string;
    city: string;
    postalCode: string;
    countrySubDivisionCode: string;
    country?: string;
  };
  printOnCheckName?: string;
  acctNum?: string;
  taxIdentifier?: string;
  active?: boolean;
  balance?: number;
}

export interface SoftDeleteVendorRequest {
  id: string;
  syncToken: string;
}

// Bill Entity
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

// Invoice Entity
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
  balance: number;
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

// Chart of Accounts Entity
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

// Journal Entry Entity
export interface QBOJournalEntryHeader {
  journalEntryId: number;
  qbJournalEntryId: string;
  qbRealmId: string;
  syncToken: string;
  domain: string | null;
  sparse: boolean | null;
  adjustment: boolean | null;
  txnDate: string | null;
  docNumber: string | null;
  privateNote: string | null;
  currencyCode: string | null;
  exchangeRate: number | null;
  totalAmount: number | null;
  homeTotalAmount: number | null;
  createTime: string | null;
  lastUpdatedTime: string | null;
  rawJson: string | null;
}

// QuickBooks Token
export interface QuickBooksToken {
  id: number;
  userId: number;
  realmId: string;
  idToken: string;
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  xRefreshTokenExpiresIn: number;
  createdAt: string;
  updatedAt: string;
}

// Dashboard Stats
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

// Navigation Item
export interface NavItem {
  title: string;
  href: string;
  icon: string;
}
