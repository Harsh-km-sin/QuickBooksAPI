import { useNavigate } from 'react-router-dom';
import { Users, Package, Truck, FileText, Receipt, BookOpen, BookText, ChevronLeft, ChevronRight, Database } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

const entities = [
  { href: '/settings/master-data/customers', icon: Users, title: 'Customers', description: 'View and manage customer records' },
  { href: '/settings/master-data/vendors', icon: Truck, title: 'Vendors', description: 'View and manage vendor records' },
  { href: '/settings/master-data/products', icon: Package, title: 'Products', description: 'View and manage products and services' },
  { href: '/settings/master-data/invoices', icon: Receipt, title: 'Invoices', description: 'View and manage customer invoices' },
  { href: '/settings/master-data/bills', icon: FileText, title: 'Bills', description: 'View and manage vendor bills' },
  { href: '/settings/master-data/chart-of-accounts', icon: BookOpen, title: 'Chart of Accounts', description: 'View and manage account categories' },
  { href: '/settings/master-data/journal-entries', icon: BookText, title: 'Journal Entries', description: 'View and manage journal entries' },
];

export function MasterData() {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/settings')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div className="bg-primary p-2 rounded-lg">
          <Database className="h-5 w-5 text-primary-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Master Data</h1>
          <p className="text-sm text-muted-foreground">View and manage your synced QuickBooks data</p>
        </div>
      </div>

      <Separator />

      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
        {entities.map(({ href, icon: Icon, title, description }) => (
          <Card
            key={href}
            className="cursor-pointer hover:border-primary hover:shadow-sm transition-all group"
            onClick={() => navigate(href)}
          >
            <CardContent className="flex items-start gap-4 p-5">
              <div className="bg-muted group-hover:bg-primary/10 p-3 rounded-lg transition-colors shrink-0">
                <Icon className="h-5 w-5 text-muted-foreground group-hover:text-primary transition-colors" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm">{title}</p>
                <p className="text-xs text-muted-foreground mt-1">{description}</p>
              </div>
              <ChevronRight className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5 group-hover:text-primary transition-colors" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
