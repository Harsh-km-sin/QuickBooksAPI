import { useState, useEffect } from 'react';
import { useLocation, Link } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import {
  Menu,
  Home,
  Users,
  Package,
  Truck,
  FileText,
  Receipt,
  BookOpen,
  BookText,
  Settings,
  LogOut,
  ChevronDown,
  Building2,
  Sun,
  Moon,
} from 'lucide-react';
import { authApi } from '@/api/client';
import { useTheme } from '@/components/theme-provider';

interface NavItem {
  title: string;
  href: string;
  icon: React.ElementType;
}

const navItems: NavItem[] = [
  { title: 'Connected Companies', href: '/', icon: Building2 },
  { title: 'Dashboard', href: '/dashboard', icon: Home },
  { title: 'Customers', href: '/customers', icon: Users },
  { title: 'Products', href: '/products', icon: Package },
  { title: 'Vendors', href: '/vendors', icon: Truck },
  { title: 'Bills', href: '/bills', icon: FileText },
  { title: 'Invoices', href: '/invoices', icon: Receipt },
  { title: 'Chart of Accounts', href: '/chart-of-accounts', icon: BookOpen },
  { title: 'Journal Entries', href: '/journal-entries', icon: BookText },
];

function Sidebar({ className }: { className?: string }) {
  const { pathname: currentPath } = useLocation();
  const { user, currentRealmId, setCurrentRealm, logout } = useAuth();
  const { theme, setTheme } = useTheme();
  const [companies, setCompanies] = useState<Array<{ qboRealmId: string; companyName: string | null; isQboConnected: boolean }>>([]);

  // Fetch connected companies from backend
  useEffect(() => {
    let mounted = true;
    authApi.getConnectedCompanies()
      .then((res) => {
        if (mounted) {
          const data = res?.data ?? res ?? [];
          if (Array.isArray(data)) {
            setCompanies(data);
          }
        }
      })
      .catch(() => {
        if (mounted) setCompanies([]);
      });
    return () => { mounted = false; };
  }, []);

  const connectedCompanies = companies.filter((c) => c.isQboConnected);
  const connectedCount = connectedCompanies.length;
  const currentCompany = companies.find((c) => c.qboRealmId === currentRealmId);
  const currentCompanyName = currentCompany?.companyName || (currentRealmId ? `${currentRealmId.slice(0, 8)}...` : null);
  
  const hasMultipleRealms = connectedCount > 1;
  const isConnected = connectedCount > 0;

  return (
    <div className={`flex flex-col h-full bg-card border-r ${className}`}>
      <div className="p-6 border-b">
        <Link to="/" className="flex items-center gap-2">
          <div className="bg-primary p-2 rounded-lg">
            <Building2 className="h-6 w-6 text-primary-foreground" />
          </div>
          <span className="text-xl font-bold">QB Connect</span>
        </Link>
      </div>

      <ScrollArea className="flex-1 py-4">
        <nav className="px-4 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = currentPath === item.href;
            return (
              <Link
                key={item.href}
                to={item.href}
                className={`flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                  isActive ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                }`}
              >
                <Icon className="h-4 w-4" />
                {item.title}
              </Link>
            );
          })}
        </nav>
      </ScrollArea>

      <div className="p-4 border-t">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium">QuickBooks</span>
          <Badge variant={isConnected ? 'default' : 'secondary'}>
            {isConnected ? 'Connected' : 'Not Connected'}
          </Badge>
        </div>
        {connectedCount !== null && connectedCount > 0 && (
          <p className="text-xs text-muted-foreground mt-1">
            {connectedCount} {connectedCount === 1 ? 'company' : 'companies'} linked
          </p>
        )}
      </div>

      <div className="p-4 border-t">
        <div className="flex items-center gap-3 mb-3">
          <Button variant="ghost" size="icon" className="hover:bg-muted hover:text-foreground" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>
            {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>
          <span className="text-sm text-muted-foreground">{theme === 'dark' ? 'Light mode' : 'Dark mode'}</span>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="w-full justify-start px-2 hover:bg-muted hover:text-foreground">
              <Avatar className="h-8 w-8 mr-2">
                <AvatarFallback>{user?.name?.charAt(0).toUpperCase() || 'U'}</AvatarFallback>
              </Avatar>
              <div className="flex-1 text-left">
                <p className="text-sm font-medium truncate">{user?.name}</p>
                {currentCompanyName && <p className="text-xs text-muted-foreground truncate">{currentCompanyName}</p>}
              </div>
              <ChevronDown className="h-4 w-4 ml-2" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel>My Account</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {hasMultipleRealms && (
              <>
                <DropdownMenuLabel className="text-xs text-muted-foreground">Switch Company</DropdownMenuLabel>
                {connectedCompanies.map((company) => (
                  <DropdownMenuItem 
                    key={company.qboRealmId} 
                    onClick={() => setCurrentRealm(company.qboRealmId)} 
                    className={currentRealmId === company.qboRealmId ? 'bg-muted' : ''}
                  >
                    <Building2 className="h-4 w-4 mr-2" />
                    <span className="truncate">{company.companyName || company.qboRealmId.slice(0, 12) + '...'}</span>
                    {currentRealmId === company.qboRealmId && <Badge variant="default" className="ml-auto">Active</Badge>}
                  </DropdownMenuItem>
                ))}
                <DropdownMenuSeparator />
              </>
            )}
            <DropdownMenuItem
              onClick={() => {}}
              className="hover:bg-muted hover:text-foreground"
            >
              <Settings className="h-4 w-4 mr-2" />
              Settings
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={logout}
              className="text-destructive hover:bg-muted hover:text-destructive"
            >
              <LogOut className="h-4 w-4 mr-2" />
              Logout
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}

export function MainLayout({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useState(false);

  return (
    <div className="flex h-screen bg-background">
      <aside className="hidden lg:flex w-64 flex-col">
        <Sidebar />
      </aside>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetTrigger asChild className="lg:hidden">
          <Button variant="ghost" size="icon" className="absolute top-4 left-4 z-50">
            <Menu className="h-6 w-6" />
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="p-0 w-64">
          <Sidebar />
        </SheetContent>
      </Sheet>

      <main className="flex min-h-0 flex-1 flex-col overflow-y-auto">
        <div className="p-6 lg:p-8">{children}</div>
      </main>
    </div>
  );
}
