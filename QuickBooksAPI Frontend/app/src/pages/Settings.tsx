import { useNavigate } from 'react-router-dom';
import { User, Database, Building2, ChevronRight, Settings as SettingsIcon } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';

const sections = [
  {
    href: '/settings/user-management',
    icon: User,
    title: 'User Management',
    description: 'Update your profile, username and password',
  },
  {
    href: '/settings/master-data',
    icon: Database,
    title: 'Master Data',
    description: 'Manage customers, vendors, products, invoices, bills, chart of accounts and journal entries',
  },
  {
    href: '/settings/connected-companies',
    icon: Building2,
    title: 'Connected Companies',
    description: 'View and manage your QuickBooks Online company connections',
  },
];

export function Settings() {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="bg-primary p-2 rounded-lg">
          <SettingsIcon className="h-5 w-5 text-primary-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Settings</h1>
          <p className="text-sm text-muted-foreground">Manage your account and application data</p>
        </div>
      </div>

      <Separator />

      <div className="flex flex-wrap gap-6">
        {sections.map(({ href, icon: Icon, title, description }) => (
          <Card
            key={href}
            className="cursor-pointer hover:border-primary hover:shadow-md transition-all group w-96"
            onClick={() => navigate(href)}
          >
            <CardContent className="flex flex-col gap-3 p-6">
              <div className="bg-muted group-hover:bg-primary/10 p-3 rounded-xl transition-colors w-fit">
                <Icon className="h-6 w-6 text-muted-foreground group-hover:text-primary transition-colors" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-base">{title}</p>
                <p className="text-sm text-muted-foreground mt-1 leading-relaxed">{description}</p>
              </div>
              <div className="flex items-center text-sm text-primary font-medium gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                Open <ChevronRight className="h-4 w-4" />
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
