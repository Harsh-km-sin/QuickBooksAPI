export interface QBOTerm {
  termId: number;
  qboTermId: string;
  realmId: string;
  syncToken?: string;
  name: string;
  active: boolean;
  type?: string;
  discountPercent?: number;
  discountDays?: number;
  dueDays?: number;
  dayOfMonthDue?: number;
  dueNextMonthDays?: number;
  createTime?: string;
  lastUpdatedTime?: string;
  rawJson?: string;
}
