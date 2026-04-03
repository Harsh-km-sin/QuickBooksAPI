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
