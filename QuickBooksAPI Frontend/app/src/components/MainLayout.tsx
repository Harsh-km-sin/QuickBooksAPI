import { useEffect, useState } from 'react';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Menu } from 'lucide-react';
import { TopNavBar } from '@/components/layout/TopNavBar';
import { AppSidebar } from '@/components/layout/AppSidebar';

const SIDEBAR_COLLAPSED_KEY = 'qb-connect-sidebar-collapsed';

export function MainLayout({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true');

  useEffect(() => {
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(collapsed));
  }, [collapsed]);

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <div className="h-screen flex flex-col bg-background">
        <TopNavBar
          menuTrigger={
            <SheetTrigger asChild className="lg:hidden">
              <Button variant="ghost" size="icon">
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
          }
          collapsed={collapsed}
          onToggleCollapse={() => setCollapsed((c) => !c)}
        />

        <div className="flex flex-1 min-h-0">
          <aside
            className={`hidden lg:flex shrink-0 bg-card border-r border-border transition-[width] duration-200 ${
              collapsed ? 'w-20' : 'w-64'
            }`}
          >
            <AppSidebar collapsed={collapsed} />
          </aside>

          <SheetContent side="left" className="p-0 w-64">
            <AppSidebar onNavigate={() => setOpen(false)} />
          </SheetContent>

          <main className="flex-1 min-w-0 overflow-y-auto">
            <div className="p-6 lg:p-8">{children}</div>
          </main>
        </div>
      </div>
    </Sheet>
  );
}
