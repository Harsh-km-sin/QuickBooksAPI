import { useLocation, Link } from 'react-router-dom';
import {
  LayoutDashboard,
  FileText,
  BookText,
  BarChart3,
  Wallet,
  Receipt,
  LineChart,
  Bot
} from 'lucide-react';

interface NavItem {
  title: string;
  href?: string;
  icon: React.ElementType;
  disabled?: boolean;
  badge?: string;
}

const mainNavItems: NavItem[] = [
  { title: 'Dashboard', href: '/', icon: LayoutDashboard },
  { title: 'Invoices', href: '/invoices', icon: FileText },
  { title: 'Journal Entries', href: '/journal-entries', icon: BookText },
  { title: 'P&L Statements', href: '/reports/profit-and-loss', icon: BarChart3 },
  { title: 'Balance Sheets', href: '/reports/balance-sheet', icon: Wallet },
  { title: 'Bills', href: '/bills', icon: Receipt },
];

const aiInsightItems: NavItem[] = [
  { title: 'Run Forecast', icon: LineChart, disabled: true, badge: 'Soon' },
  { title: 'AI Assistant', icon: Bot, disabled: true, badge: 'Soon' },
];

function NavRow({
  item,
  isActive,
  onNavigate,
  collapsed,
}: {
  item: NavItem;
  isActive: boolean;
  onNavigate?: () => void;
  collapsed?: boolean;
}) {
  const content = (
    <>
      <item.icon className="h-4 w-4 shrink-0" />
      {!collapsed && <span className="text-sm">{item.title}</span>}
      {!collapsed && item.badge && (
        <span className="ml-auto text-[10px] font-bold uppercase bg-muted px-1.5 py-0.5 rounded">
          {item.badge}
        </span>
      )}
    </>
  );

  if (item.disabled || !item.href) {
    return (
      <div
        title={collapsed ? item.title : undefined}
        className={`flex items-center gap-3 py-3 rounded-lg text-muted-foreground opacity-50 cursor-not-allowed ${collapsed ? 'justify-center px-0' : 'px-3'
          }`}
      >
        {content}
      </div>
    );
  }

  return (
    <Link
      to={item.href}
      onClick={onNavigate}
      title={collapsed ? item.title : undefined}
      className={`flex items-center gap-3 py-3 rounded-lg text-sm font-medium transition-colors ${collapsed ? 'justify-center px-0' : 'px-3'
        } ${isActive ? 'bg-secondary text-secondary-foreground font-bold' : 'text-muted-foreground hover:bg-muted hover:text-foreground'
        }`}
    >
      {content}
    </Link>
  );
}

export function AppSidebar({
  onNavigate,
  className,
  collapsed,
}: {
  onNavigate?: () => void;
  className?: string;
  collapsed?: boolean;
}) {
  const { pathname } = useLocation();

  const isItemActive = (href?: string) =>
    !!href && (pathname === href || (href !== '/' && pathname.startsWith(href + '/')));

  return (
    <div className={`flex flex-col h-full w-full bg-card pt-4 ${className ?? ''}`}>
      {/* <Link
        to="/"
        onClick={onNavigate}
        className={`flex items-center gap-3 py-4 mb-2 shrink-0 min-w-0 ${collapsed ? 'justify-center px-0' : 'px-3'}`}
      >
        <div
          className={`bg-primary rounded-lg flex items-center justify-center text-primary-foreground shadow-sm shrink-0 ${
            collapsed ? 'w-12 h-12' : 'w-10 h-10'
          }`}
        >
          <Landmark className={collapsed ? 'h-6 w-6' : 'h-5 w-5'} />
        </div>
        {!collapsed && (
          <div className="min-w-0">
            <p className="font-bold text-sm text-primary leading-tight truncate">Professional Hub</p>
            <p className="text-xs text-muted-foreground">Accounting Suite</p>
          </div>
        )}
      </Link> */}

      {/* <Button
        className={`mb-4 rounded-xl font-bold shrink-0 ${collapsed ? 'h-10 w-10 self-center p-0' : 'h-11 mx-3'}`}
        onClick={() => toast.info('Quick create is not available yet')}
        title={collapsed ? 'Create New' : undefined}
      >
        <Plus className="h-4 w-4" />
        {!collapsed && 'Create New'}
      </Button> */}

      <nav className="flex-1 flex flex-col gap-1 px-3 overflow-y-auto overflow-x-hidden">
        {mainNavItems.map((item) => (
          <NavRow key={item.title} item={item} isActive={isItemActive(item.href)} onNavigate={onNavigate} collapsed={collapsed} />
        ))}

        {collapsed ? (
          <div className="mt-6 mb-1 mx-3 border-t border-border" />
        ) : (
          <p className="mt-6 mb-1 px-3 text-[10px] font-bold uppercase tracking-widest text-muted-foreground opacity-70">
            AI Insights
          </p>
        )}
        {aiInsightItems.map((item) => (
          <NavRow key={item.title} item={item} isActive={false} collapsed={collapsed} />
        ))}
      </nav>
    </div>
  );
}
