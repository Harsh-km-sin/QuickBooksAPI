import type { ReactNode } from 'react';
import { StandaloneHeader } from './StandaloneHeader';
import { StandaloneFooter } from './StandaloneFooter';
import { cn } from '@/lib/utils';

interface StandalonePageShellProps {
  headerRight?: ReactNode;
  children: ReactNode;
  contentClassName?: string;
}

export function StandalonePageShell({ headerRight, children, contentClassName }: StandalonePageShellProps) {
  return (
    <div className="h-screen flex flex-col bg-background">
      <StandaloneHeader right={headerRight} />
      <main className="flex-1 min-h-0 overflow-y-auto">
        <div className={cn('min-h-full flex items-center justify-center px-4 py-8', contentClassName)}>
          {children}
        </div>
      </main>
      <StandaloneFooter />
    </div>
  );
}
