import { format, parse, isValid } from 'date-fns';
import { CalendarIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
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
          startMonth={fromDate}
          endMonth={toDate}
          disabled={
            fromDate || toDate
              ? (date) => (fromDate ? date < fromDate : false) || (toDate ? date > toDate : false)
              : undefined
          }
          autoFocus
        />
      </PopoverContent>
    </Popover>
  );
}
