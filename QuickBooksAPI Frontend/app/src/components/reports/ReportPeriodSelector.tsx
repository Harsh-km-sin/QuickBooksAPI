import { useState, useEffect } from 'react';
import { DatePicker, parseApiDate } from '@/components/ui/date-picker';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import {
  REPORT_PERIOD_PRESETS,
  getPeriodPresetRange,
  type ReportPeriodPreset,
} from '@/utils/reportPeriodPresets';
import { AccountingMethodSwitch, type AccountingMethod } from './AccountingMethodSwitch';

interface ReportPeriodSelectorProps {
  startDate: string;
  endDate: string;
  onRangeChange: (start: string, end: string) => void;
  accountingMethod: AccountingMethod;
  onAccountingMethodChange: (method: AccountingMethod) => void;
  isPointInTime?: boolean;
  disabled?: boolean;
}

export function ReportPeriodSelector({
  startDate,
  endDate,
  onRangeChange,
  accountingMethod,
  onAccountingMethodChange,
  isPointInTime = false,
  disabled = false,
}: ReportPeriodSelectorProps) {
  const [preset, setPreset] = useState<ReportPeriodPreset>('Custom dates');

  const handlePresetSelect = (selected: ReportPeriodPreset) => {
    setPreset(selected);
    if (selected !== 'Custom dates') {
      const range = getPeriodPresetRange(selected);
      onRangeChange(range.startDate, range.endDate);
    }
  };

  return (
    <div className="flex flex-col md:flex-row md:items-end gap-4">
      {/* Report period preset selector */}
      <div className="space-y-2 min-w-[11rem]">
        <Label htmlFor="reportPeriod">Report period</Label>
        <Select
          value={preset}
          onValueChange={(val) => handlePresetSelect(val as ReportPeriodPreset)}
          disabled={disabled}
        >
          <SelectTrigger id="reportPeriod" className="w-full font-medium">
            <SelectValue placeholder="Select period" />
          </SelectTrigger>
          <SelectContent>
            {REPORT_PERIOD_PRESETS.map((p) => (
              <SelectItem key={p} value={p}>
                {p}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* From Date Picker (hidden for point-in-time like Balance Sheet unless custom) */}
      {!isPointInTime && (
        <div className="space-y-2">
          <Label htmlFor="startDate">From</Label>
          <DatePicker
            id="startDate"
            value={startDate}
            onChange={(d) => {
              setPreset('Custom dates');
              onRangeChange(d, endDate);
            }}
            disabled={disabled || preset !== 'Custom dates'}
            toDate={parseApiDate(endDate)}
          />
        </div>
      )}

      {/* To / As of Date Picker */}
      <div className="space-y-2">
        <Label htmlFor="endDate">{isPointInTime ? 'As of' : 'To'}</Label>
        <DatePicker
          id="endDate"
          value={endDate}
          onChange={(d) => {
            setPreset('Custom dates');
            onRangeChange(isPointInTime ? d : startDate, d);
          }}
          disabled={disabled || preset !== 'Custom dates'}
          fromDate={!isPointInTime && preset === 'Custom dates' ? parseApiDate(startDate) : undefined}
        />
      </div>

      {/* Accounting Method Pill Switch */}
      <AccountingMethodSwitch
        value={accountingMethod}
        onChange={onAccountingMethodChange}
        disabled={disabled}
      />
    </div>
  );
}
