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
