import type { ReactNode } from 'react';
import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';

export interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  /** Extra controls rendered after the input — filter selects, count badges, etc. */
  rightSlot?: ReactNode;
}

export function SearchBar({ value, onChange, placeholder = 'Search...', className, rightSlot }: SearchBarProps) {
  return (
    <div className={cn('flex items-center gap-4', className)}>
      <div className="relative flex-1 max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder={placeholder}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="pl-9"
        />
      </div>
      {rightSlot}
    </div>
  );
}
