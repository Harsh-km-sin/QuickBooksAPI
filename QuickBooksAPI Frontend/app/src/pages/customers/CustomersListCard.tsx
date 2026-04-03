import type { Customer } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Search,
  Plus,
  MoreHorizontal,
  Edit,
  Trash2,
  Mail,
  Phone,
  MapPin,
  Users,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;

export interface CustomersListCardProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  activeFilter: 'active' | 'inactive' | 'all';
  onActiveFilterChange: (value: 'active' | 'inactive' | 'all') => void;
  totalCount: number;
  customers: Customer[];
  debouncedSearch: string;
  onOpenCreate: () => void;
  formatCurrency: (value: number) => string;
  onOpenEdit: (customer: Customer) => void;
  onOpenDelete: (customer: Customer) => void;
  isLoadingCustomer: boolean;
  currentPage: number;
  pageSize: number;
  onPageSizeChange: (size: number) => void;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onGoToPage: (page: number) => void;
}

export function CustomersListCard({
  searchTerm,
  onSearchChange,
  activeFilter,
  onActiveFilterChange,
  totalCount,
  customers,
  debouncedSearch,
  onOpenCreate,
  formatCurrency,
  onOpenEdit,
  onOpenDelete,
  isLoadingCustomer,
  currentPage,
  pageSize,
  onPageSizeChange,
  totalPages,
  hasNextPage,
  hasPreviousPage,
  onGoToPage,
}: CustomersListCardProps) {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search customers..."
              value={searchTerm}
              onChange={(e) => onSearchChange(e.target.value)}
              className="pl-9"
            />
          </div>
          <Select value={activeFilter} onValueChange={(value) => onActiveFilterChange(value as 'active' | 'inactive' | 'all')}>
            <SelectTrigger className="w-[140px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="inactive">Inactive</SelectItem>
              <SelectItem value="all">All</SelectItem>
            </SelectContent>
          </Select>
          <Badge variant="default">
            {totalCount} customer{totalCount !== 1 ? 's' : ''}
          </Badge>
        </div>
      </CardHeader>
      <CardContent>
        {customers.length === 0 ? (
          <div className="text-center py-12">
            <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <h3 className="text-lg font-semibold mb-2">No customers found</h3>
            <p className="text-muted-foreground mb-4">
              {debouncedSearch ? 'Try adjusting your search' : 'Get started by adding your first customer'}
            </p>
            {!debouncedSearch && (
              <Button onClick={onOpenCreate}>
                <Plus className="h-4 w-4 mr-2" />
                Add Customer
              </Button>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Company</TableHead>
                  <TableHead>Contact</TableHead>
                  <TableHead>Balance</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-[50px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {customers.map((customer) => (
                  <TableRow key={customer.id}>
                    <TableCell>
                      <div className="font-medium">{customer.displayName || `${customer.givenName} ${customer.familyName}`}</div>
                      {customer.displayName && customer.givenName && (
                        <div className="text-sm text-muted-foreground">
                          {customer.givenName} {customer.familyName}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>{customer.companyName || '-'}</TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        {customer.primaryEmailAddr && (
                          <div className="flex items-center text-sm">
                            <Mail className="h-3 w-3 mr-1 text-muted-foreground" />
                            {customer.primaryEmailAddr}
                          </div>
                        )}
                        {customer.primaryPhone && (
                          <div className="flex items-center text-sm">
                            <Phone className="h-3 w-3 mr-1 text-muted-foreground" />
                            {customer.primaryPhone}
                          </div>
                        )}
                        {customer.billAddrLine1 && (
                          <div className="flex items-center text-sm text-muted-foreground">
                            <MapPin className="h-3 w-3 mr-1" />
                            {customer.billAddrCity}, {customer.billAddrCountrySubDivisionCode}
                          </div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className={customer.balance > 0 ? 'text-destructive' : ''}>
                        {formatCurrency(customer.balance)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Badge variant={customer.active ? 'default' : 'secondary'}>
                        {customer.active ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => onOpenEdit(customer)} disabled={isLoadingCustomer}>
                            <Edit className="h-4 w-4 mr-2" />
                            Edit
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            onClick={() => onOpenDelete(customer)}
                            className="text-destructive"
                          >
                            <Trash2 className="h-4 w-4 mr-2" />
                            Delete
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
        {totalCount > 0 && (
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 border-t px-4 py-3">
            <div className="flex items-center gap-4">
              <p className="text-sm text-muted-foreground">
                Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
              </p>
              <div className="flex items-center gap-2">
                <span className="text-sm text-muted-foreground whitespace-nowrap">Per page</span>
                <Select
                  value={String(pageSize)}
                  onValueChange={(value) => onPageSizeChange(Number(value))}
                >
                  <SelectTrigger className="w-[70px]" size="sm">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {PAGE_SIZE_OPTIONS.map((size) => (
                      <SelectItem key={size} value={String(size)}>
                        {size}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => onGoToPage(currentPage - 1)}
                disabled={!hasPreviousPage}
              >
                <ChevronLeft className="h-4 w-4 mr-1" />
                Previous
              </Button>
              <span className="text-sm text-muted-foreground">
                Page {currentPage} of {totalPages || 1}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => onGoToPage(currentPage + 1)}
                disabled={!hasNextPage}
              >
                Next
                <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
