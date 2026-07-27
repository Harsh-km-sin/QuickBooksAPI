export type ReportPeriodPreset =
  | 'All Dates'
  | 'Custom dates'
  | 'Today'
  | 'This week'
  | 'This week to date'
  | 'This fiscal week'
  | 'This month'
  | 'This month to date'
  | 'This quarter'
  | 'This quarter to date'
  | 'This fiscal quarter'
  | 'This fiscal quarter to date'
  | 'This year'
  | 'This year to date';

export const REPORT_PERIOD_PRESETS: ReportPeriodPreset[] = [
  'All Dates',
  'Custom dates',
  'Today',
  'This week',
  'This week to date',
  'This fiscal week',
  'This month',
  'This month to date',
  'This quarter',
  'This quarter to date',
  'This fiscal quarter',
  'This fiscal quarter to date',
  'This year',
  'This year to date',
];

function formatDate(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/**
 * Custom function calculating start and end dates for report period presets.
 */
export function getPeriodPresetRange(
  preset: ReportPeriodPreset,
  fiscalYearStartMonth: number = 1
): { startDate: string; endDate: string } {
  const now = new Date();
  const todayStr = formatDate(now);

  switch (preset) {
    case 'All Dates':
      return { startDate: '1970-01-01', endDate: todayStr };

    case 'Today':
      return { startDate: todayStr, endDate: todayStr };

    case 'This week': {
      const day = now.getDay(); // 0 (Sun) to 6 (Sat)
      const start = new Date(now);
      start.setDate(now.getDate() - day);
      const end = new Date(start);
      end.setDate(start.getDate() + 6);
      return { startDate: formatDate(start), endDate: formatDate(end) };
    }

    case 'This week to date': {
      const day = now.getDay();
      const start = new Date(now);
      start.setDate(now.getDate() - day);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'This fiscal week': {
      // Fiscal week aligns with Sunday-start week
      const day = now.getDay();
      const start = new Date(now);
      start.setDate(now.getDate() - day);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'This month': {
      const start = new Date(now.getFullYear(), now.getMonth(), 1);
      const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
      return { startDate: formatDate(start), endDate: formatDate(end) };
    }

    case 'This month to date': {
      const start = new Date(now.getFullYear(), now.getMonth(), 1);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'This quarter': {
      const currentMonth = now.getMonth();
      const quarterStartMonth = Math.floor(currentMonth / 3) * 3;
      const start = new Date(now.getFullYear(), quarterStartMonth, 1);
      const end = new Date(now.getFullYear(), quarterStartMonth + 3, 0);
      return { startDate: formatDate(start), endDate: formatDate(end) };
    }

    case 'This quarter to date': {
      const currentMonth = now.getMonth();
      const quarterStartMonth = Math.floor(currentMonth / 3) * 3;
      const start = new Date(now.getFullYear(), quarterStartMonth, 1);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'This fiscal quarter': {
      const fyStart = fiscalYearStartMonth - 1; // 0-indexed
      let monthOffset = (now.getMonth() - fyStart + 12) % 12;
      const fiscalQuarterIndex = Math.floor(monthOffset / 3);
      const startMonth = (fyStart + fiscalQuarterIndex * 3) % 12;
      const startYear = now.getFullYear() - (now.getMonth() < fyStart && startMonth >= fyStart ? 1 : 0);
      const start = new Date(startYear, startMonth, 1);
      const end = new Date(startYear, startMonth + 3, 0);
      return { startDate: formatDate(start), endDate: formatDate(end) };
    }

    case 'This fiscal quarter to date': {
      const fyStart = fiscalYearStartMonth - 1;
      let monthOffset = (now.getMonth() - fyStart + 12) % 12;
      const fiscalQuarterIndex = Math.floor(monthOffset / 3);
      const startMonth = (fyStart + fiscalQuarterIndex * 3) % 12;
      const startYear = now.getFullYear() - (now.getMonth() < fyStart && startMonth >= fyStart ? 1 : 0);
      const start = new Date(startYear, startMonth, 1);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'This year': {
      const start = new Date(now.getFullYear(), 0, 1);
      const end = new Date(now.getFullYear(), 11, 31);
      return { startDate: formatDate(start), endDate: formatDate(end) };
    }

    case 'This year to date': {
      const start = new Date(now.getFullYear(), 0, 1);
      return { startDate: formatDate(start), endDate: todayStr };
    }

    case 'Custom dates':
    default:
      const defaultStart = new Date(now.getFullYear(), now.getMonth(), 1);
      return { startDate: formatDate(defaultStart), endDate: todayStr };
  }
}
