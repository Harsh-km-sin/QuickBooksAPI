import type { ReactNode } from 'react';
import { Building2 } from 'lucide-react';

export function StandaloneHeader({ right }: { right?: ReactNode }) {
  return (
    <header className="shrink-0 w-full bg-card border-b border-border shadow-sm">
      <div className="flex items-center justify-between px-4 md:px-8 max-w-[1440px] mx-auto h-16">
        <div className="flex items-center gap-2">
          <div className="bg-primary p-1.5 rounded-lg">
            <Building2 className="h-5 w-5 text-primary-foreground" />
          </div>
          <span className="font-bold text-lg text-primary">QuickBooks Professional</span>
        </div>
        {right}
      </div>
    </header>
  );
}
