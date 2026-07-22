import { useState } from 'react';
import {
  format,
  parse,
  isValid,
  addMonths,
  startOfDay,
  endOfDay,
  startOfMonth,
  startOfYear,
  endOfYear,
  setMonth as setMonthOfDate,
  setYear as setYearOfDate,
} from 'date-fns';
import { CalendarIcon, ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react';
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

function clamp(date: Date, min: Date, max: Date): Date {
  if (date < min) return min;
  if (date > max) return max;
  return date;
}

/** Which chooser the popover is showing. The day grid is the resting state. */
type View = 'days' | 'months' | 'years';

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
 *
 * The month and year choosers are panels that take over the popover body rather than dropdowns.
 * A dropdown would have to render its list in a portal outside the popover, which the popover
 * then reads as an outside click and dismisses itself — two dismissable layers fighting over the
 * same click. Swapping the body keeps every control inside one layer, so there is nothing to
 * coordinate.
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

  // What the month/year choosers may navigate to. Deliberately independent of fromDate/toDate:
  // those bound which dates are *selectable*, and folding them in here would shrink navigation
  // to the selectable window — a "To" picker bounded below by the start date would offer a
  // single month and a single year. Ten years back matches the backend's maximum backfill
  // depth; the current year is the upper bound because a future date can never have report data.
  const currentYear = new Date().getFullYear();
  let navStart = new Date(currentYear - 10, 0, 1);
  let navEnd = new Date(currentYear, 11, 31);

  // A value outside that window must still be reachable, or the calendar cannot show what the
  // trigger says is selected.
  if (selected && selected < navStart) navStart = startOfYear(selected);
  if (selected && selected > navEnd) navEnd = endOfYear(selected);

  // Selectability, on the other hand, is exactly what the caller asked for. Days outside the
  // range stay visible but greyed, so it is obvious *why* they cannot be picked.
  const isDisabled = (date: Date) =>
    (fromDate ? date < startOfDay(fromDate) : false) ||
    (toDate ? date > endOfDay(toDate) : date > endOfDay(new Date()));

  const [open, setOpen] = useState(false);
  const [view, setView] = useState<View>('days');
  const [month, setMonth] = useState(() => startOfMonth(clamp(selected ?? new Date(), navStart, navEnd)));

  const goToMonth = (next: Date) => setMonth(startOfMonth(clamp(next, navStart, navEnd)));

  const canGoBack = startOfMonth(month) > startOfMonth(navStart);
  const canGoForward = startOfMonth(month) < startOfMonth(navEnd);

  const years: number[] = [];
  for (let year = navEnd.getFullYear(); year >= navStart.getFullYear(); year--) years.push(year);

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        // Reopening always lands on the day grid, showing the selected date's month rather than
        // wherever the last session happened to browse to.
        if (next) {
          setView('days');
          goToMonth(selected ?? new Date());
        }
      }}
    >
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
      <PopoverContent className="w-auto p-3" align="start">
        <div className="flex items-center justify-between gap-1 pb-2">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn('size-8', view !== 'days' && 'invisible')}
            disabled={!canGoBack}
            aria-label="Previous month"
            onClick={() => goToMonth(addMonths(month, -1))}
          >
            <ChevronLeft className="size-4" />
          </Button>

          <div className="flex items-center gap-1">
            <CaptionButton
              label={format(month, 'MMMM')}
              expanded={view === 'months'}
              onClick={() => setView(view === 'months' ? 'days' : 'months')}
            />
            <CaptionButton
              label={format(month, 'yyyy')}
              expanded={view === 'years'}
              onClick={() => setView(view === 'years' ? 'days' : 'years')}
            />
          </div>

          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn('size-8', view !== 'days' && 'invisible')}
            disabled={!canGoForward}
            aria-label="Next month"
            onClick={() => goToMonth(addMonths(month, 1))}
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>

        {view === 'days' && (
          <Calendar
            // Slightly roomier than the component default, and unpadded because the popover
            // already pads the shared header and body.
            className="p-0 [--cell-size:2.5rem]"
            mode="single"
            selected={selected}
            month={month}
            onMonthChange={goToMonth}
            onSelect={(date) => {
              if (date) {
                onChange(toApiDate(date));
                setOpen(false);
              }
            }}
            // The header above replaces both, so react-day-picker must not draw its own.
            hideNavigation
            components={{ MonthCaption: () => <></> }}
            startMonth={navStart}
            endMonth={navEnd}
            disabled={isDisabled}
            autoFocus
          />
        )}

        {view === 'months' && (
          <ChooserGrid
            items={Array.from({ length: 12 }, (_, index) => ({
              key: index,
              label: format(new Date(month.getFullYear(), index, 1), 'MMM'),
              selected: month.getMonth() === index,
              // A month is reachable only if it falls inside the navigable window.
              disabled:
                new Date(month.getFullYear(), index, 1) > navEnd ||
                new Date(month.getFullYear(), index + 1, 0) < navStart,
              onSelect: () => {
                goToMonth(setMonthOfDate(month, index));
                setView('days');
              },
            }))}
          />
        )}

        {view === 'years' && (
          <ChooserGrid
            // Newest first: reports are usually run against the current or previous year, so the
            // common choices sit at the top without scrolling.
            items={years.map((year) => ({
              key: year,
              label: String(year),
              selected: month.getFullYear() === year,
              disabled: false,
              onSelect: () => {
                goToMonth(setYearOfDate(month, year));
                setView('days');
              },
            }))}
          />
        )}
      </PopoverContent>
    </Popover>
  );
}

function CaptionButton({
  label,
  expanded,
  onClick,
}: {
  label: string;
  expanded: boolean;
  onClick: () => void;
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      aria-expanded={expanded}
      onClick={onClick}
      className="h-8 gap-1 px-2 text-sm font-medium"
    >
      {label}
      <ChevronDown
        className={cn('size-3.5 opacity-60 transition-transform', expanded && 'rotate-180')}
      />
    </Button>
  );
}

interface ChooserItem {
  key: number;
  label: string;
  selected: boolean;
  disabled: boolean;
  onSelect: () => void;
}

/** The month and year panels: same grid, different contents. */
function ChooserGrid({ items }: { items: ChooserItem[] }) {
  return (
    // Width matches the day grid (7 cells of --cell-size) so switching views does not resize the
    // popover. Capped and scrollable so a long year list cannot stretch it off screen.
    <div className="grid max-h-[15rem] w-[17.5rem] grid-cols-3 gap-2 overflow-y-auto">
      {items.map((item) => (
        <Button
          key={item.key}
          type="button"
          variant={item.selected ? 'default' : 'ghost'}
          disabled={item.disabled}
          onClick={item.onSelect}
          className="h-9 font-normal"
        >
          {item.label}
        </Button>
      ))}
    </div>
  );
}
