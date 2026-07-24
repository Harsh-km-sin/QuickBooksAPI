import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth';
import { useConnectedCompanies } from '@/features/company';
import { useTheme } from '@/components/theme-provider';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Search,
  Bell,
  HelpCircle,
  Sun,
  Moon,
  LogOut,
  Building2,
  ChevronDown,
  Settings,
  PanelLeftClose,
  PanelLeftOpen,
} from 'lucide-react';
import { toast } from 'sonner';

export function TopNavBar({
  menuTrigger,
  collapsed,
  onToggleCollapse,
}: {
  menuTrigger?: ReactNode;
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}) {
  const navigate = useNavigate();
  const { user, currentRealmId, setCurrentRealm, logout } = useAuth();
  const { theme, setTheme } = useTheme();
  const { companies } = useConnectedCompanies({ silent: true });

  const connectedCompanies = companies.filter((c) => c.isQboConnected);
  const hasMultipleRealms = connectedCompanies.length > 1;

  return (
    <header className="shrink-0 w-full h-16 flex items-center gap-4 px-4 md:px-8 bg-card border-b border-border shadow-sm z-50">
      <div className="flex items-center gap-2 shrink-0 min-w-0">
        {menuTrigger}
        <span className="text-lg font-bold text-primary truncate">QuickBooks Professional</span>
        {onToggleCollapse && (
          <Button
            variant="ghost"
            size="icon"
            className="hidden lg:inline-flex h-8 w-8 text-muted-foreground hover:text-foreground"
            onClick={onToggleCollapse}
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {collapsed ? <PanelLeftOpen className="h-4 w-4" /> : <PanelLeftClose className="h-4 w-4" />}
          </Button>
        )}
      </div>

      <div className="hidden md:flex flex-1 justify-center px-4">
        <div className="relative w-full max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search clients, transactions, reports..."
            className="bg-muted border-none rounded-full pl-10 pr-4 h-9 w-full focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>
      </div>

      <div className="flex items-center gap-1 shrink-0">
        <Button
          variant="ghost"
          size="icon"
          className="rounded-full hover:bg-accent"
          onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
        >
          {theme === 'dark' ? <Sun className="h-4 w-4 text-primary" /> : <Moon className="h-4 w-4 text-primary" />}
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="rounded-full hover:bg-accent"
          onClick={() => toast.info('Notifications are not available yet')}
        >
          <Bell className="h-4 w-4 text-primary" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="rounded-full hover:bg-accent"
          onClick={() => toast.info('Help center is not available yet')}
        >
          <HelpCircle className="h-4 w-4 text-primary" />
        </Button>

        <div className="h-8 w-px bg-border mx-2" />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className="flex items-center gap-2 cursor-pointer group px-1 outline-none focus:outline-none focus-visible:outline-none">
              <Avatar className="h-8 w-8 border border-border">
                <AvatarFallback>{user?.name?.charAt(0).toUpperCase() || 'U'}</AvatarFallback>
              </Avatar>
              <span className="hidden sm:inline text-sm font-medium text-muted-foreground group-hover:text-foreground">Logout</span>
              <ChevronDown className="hidden sm:inline h-3.5 w-3.5 text-muted-foreground" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            align="end"
            className="w-56 p-1.5 rounded-xl border border-border/40 bg-popover/95 backdrop-blur-md shadow-lg space-y-0.5"
          >
            <div className="px-2.5 py-2">
              <p className="text-sm font-semibold text-foreground leading-none">{user?.name || 'My Account'}</p>
              <p className="text-[11px] text-muted-foreground mt-1 truncate">Admin User</p>
            </div>
            
            <DropdownMenuSeparator className="bg-border/40" />
            
            {hasMultipleRealms && (
              <>
                <div className="px-2.5 py-1">
                  <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/60">Switch Company</p>
                </div>
                {connectedCompanies.map((company) => (
                  <DropdownMenuItem
                    key={company.qboRealmId}
                    onClick={() => setCurrentRealm(company.qboRealmId)}
                    className={`rounded-lg px-2.5 py-2 cursor-pointer transition-colors ${
                      currentRealmId === company.qboRealmId ? 'bg-muted font-medium' : ''
                    }`}
                  >
                    <Building2 className="h-4 w-4 mr-2 text-muted-foreground" />
                    <span className="truncate">{company.companyName || company.qboRealmId.slice(0, 12) + '...'}</span>
                    {currentRealmId === company.qboRealmId && <Badge className="ml-auto">Active</Badge>}
                  </DropdownMenuItem>
                ))}
                <DropdownMenuSeparator className="bg-border/40" />
              </>
            )}
            
            <DropdownMenuItem
              onClick={() => navigate('/settings')}
              className="rounded-lg px-2.5 py-2 cursor-pointer transition-colors focus:bg-accent focus:text-accent-foreground"
            >
              <Settings className="h-4 w-4 mr-2 text-muted-foreground" />
              Settings
            </DropdownMenuItem>
            
            <DropdownMenuSeparator className="bg-border/40" />
            
            <DropdownMenuItem
              onClick={logout}
              variant="destructive"
              className="rounded-lg px-2.5 py-2 cursor-pointer transition-colors"
            >
              <LogOut className="h-4 w-4 mr-2" />
              Logout
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
