import { format, parse, isValid } from 'date-fns';
import { CalendarIcon } from 'lucide-react';
import type { DropdownProps } from 'react-day-picker';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';

/** The wire format used by the reports API. */
const API_DATE_FORMAT = 'yyyy-MM-dd';

/** What the user sees on the trigger. */
const DISPLAY_DATE_FORMAT = 'd MMM yyyy';

export function parseApiDate(value: string | undefined): Date | undefined {
  if (!value) return undefined;
  const parsed = parse(value, API_DATE_FORMAT, new Date());
  return isValid(parsed) ? parsed : undefined;
}

export function toApiDate(date: Date): string {
  // Formatted from local parts, never via toISOString(), which would shift the day
  // backwards for anyone east of UTC.
  return format(date, API_DATE_FORMAT);
}

/**
 * Month/year dropdown for the calendar caption.
 *
 * react-day-picker renders a native <select> by default, and a browser paints that popup list
 * itself — it cannot be themed. Swapping in the app's Select keeps the caption consistent with
 * everything else and gives the year list a scroll cap.
 */
function CalendarDropdown({ options, value, onChange, 'aria-label': ariaLabel }: DropdownProps) {
  const selected = options?.find((option) => option.value === Number(value));

  return (
    <Select
      value={String(value)}
      onValueChange={(next) => {
        // react-day-picker expects a change event; it only reads target.value.
        onChange?.({ target: { value: next } } as React.ChangeEvent<HTMLSelectElement>);
      }}
    >
      <SelectTrigger
        aria-label={ariaLabel}
        className="h-8 w-auto gap-1 border-none px-2 text-sm font-medium shadow-none focus:ring-0 focus:ring-offset-0"
      >
        <SelectValue>{selected?.label}</SelectValue>
      </SelectTrigger>
      <SelectContent className="max-h-60">
        {options?.map((option) => (
          <SelectItem
            key={option.value}
            value={String(option.value)}
            disabled={option.disabled}
          >
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

interface DatePickerProps {
  /** yyyy-MM-dd */
  value?: string;
  /** Receives yyyy-MM-dd. */
  onChange: (value: string) => void;
  id?: string;
  disabled?: boolean;
  placeholder?: string;
  /** Dates outside this range are not selectable. */
  fromDate?: Date;
  toDate?: Date;
  className?: string;
}

/**
 * Themed replacement for <input type="date">, whose calendar is rendered by the browser and
 * cannot be styled to match the app.
 */
export function DatePicker({
  value,
  onChange,
  id,
  disabled,
  placeholder = 'Pick a date',
  fromDate,
  toDate,
  className,
}: DatePickerProps) {
  const selected = parseApiDate(value);

  // Without an explicit range the year dropdown offers only the current year, which is useless
  // for reports that go back years. Ten years matches the backend's maximum backfill depth, and
  // a future date can never have report data, so today is the natural upper bound.
  const navigationStart = fromDate ?? new Date(new Date().getFullYear() - 10, 0, 1);
  const navigationEnd = toDate ?? new Date();

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          id={id}
          type="button"
          variant="outline"
          disabled={disabled}
          className={cn(
            'w-full justify-start text-left font-normal sm:w-[180px]',
            !selected && 'text-muted-foreground',
            className
          )}
        >
          <CalendarIcon className="h-4 w-4 shrink-0 opacity-70" />
          <span className="truncate">
            {selected ? format(selected, DISPLAY_DATE_FORMAT) : placeholder}
          </span>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          // Slightly roomier than the component default so month/year dropdowns and the
          // nav arrows sit on one line without crowding each other.
          className="[--cell-size:2.5rem]"
          mode="single"
          selected={selected}
          defaultMonth={selected}
          onSelect={(date) => {
            if (date) onChange(toApiDate(date));
          }}
          // Month/year dropdowns matter here: reports go back years, and paging a month at a
          // time to reach 2023 would be miserable.
          captionLayout="dropdown"
          // Replaces the whole caption dropdown, including react-day-picker's own label and
          // chevron, so nothing is rendered twice.
          components={{ Dropdown: CalendarDropdown }}
          startMonth={navigationStart}
          endMonth={navigationEnd}
          disabled={(date) => date < navigationStart || date > navigationEnd}
          autoFocus
        />
      </PopoverContent>
    </Popover>
  );
}
