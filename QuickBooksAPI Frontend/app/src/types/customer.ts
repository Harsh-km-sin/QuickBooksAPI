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
