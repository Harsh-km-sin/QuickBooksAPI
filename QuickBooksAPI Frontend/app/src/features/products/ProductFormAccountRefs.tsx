import { useFormContext } from 'react-hook-form';
import { Checkbox } from '@/components/ui/checkbox';
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  FormDescription,
} from '@/components/ui/form';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { BookOpen } from 'lucide-react';
import type { ChartOfAccounts } from '@/types';
import type { ProductFormValues } from './productFormSchema';

export interface ProductFormAccountRefsProps {
  iSell: boolean;
  iPurchase: boolean;
  onSellChange: (checked: boolean) => void;
  onPurchaseChange: (checked: boolean) => void;
  incomeAccounts: ChartOfAccounts[];
  expenseAccounts: ChartOfAccounts[];
  assetAccounts: ChartOfAccounts[];
  isLoadingAccounts: boolean;
}

export function ProductFormAccountRefs({
  iSell,
  iPurchase,
  onSellChange,
  onPurchaseChange,
  incomeAccounts,
  expenseAccounts,
  assetAccounts,
  isLoadingAccounts,
}: ProductFormAccountRefsProps) {
  const form = useFormContext<ProductFormValues>();
  const productType = form.watch('type');
  const isInventory = productType === 'Inventory';
  const isServiceOrNonInventory = productType === 'Service' || productType === 'NonInventory';

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
        <BookOpen className="h-4 w-4" />
        Account References
      </div>

      {isServiceOrNonInventory && (
        <div className="space-y-3">
          <div className="flex items-center space-x-2">
            <Checkbox
              id="iSell"
              checked={iSell}
              onCheckedChange={(checked) => onSellChange(checked === true)}
            />
            <label htmlFor="iSell" className="text-sm font-medium leading-none cursor-pointer">
              I sell this product/service to my customers.
            </label>
          </div>
          <div className="flex items-center space-x-2">
            <Checkbox
              id="iPurchase"
              checked={iPurchase}
              onCheckedChange={(checked) => onPurchaseChange(checked === true)}
            />
            <label htmlFor="iPurchase" className="text-sm font-medium leading-none cursor-pointer">
              I purchase this product/service from a supplier.
            </label>
          </div>
        </div>
      )}

      {(isInventory || iSell) && (
        <FormField
          control={form.control}
          name="incomeAccountRefValue"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Income Account *</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger disabled={isLoadingAccounts} className="w-full">
                    <SelectValue placeholder={isLoadingAccounts ? 'Loading accounts...' : 'Select income account'} />
                  </SelectTrigger>
                </FormControl>
                <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                  {incomeAccounts.map((account) => (
                    <SelectItem key={account.qboId} value={account.qboId}>
                      {account.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
      )}

      {(isInventory || iPurchase) && (
        <FormField
          control={form.control}
          name="expenseAccountRefValue"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Expense Account *</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger disabled={isLoadingAccounts} className="w-full">
                    <SelectValue placeholder={isLoadingAccounts ? 'Loading accounts...' : 'Select expense account'} />
                  </SelectTrigger>
                </FormControl>
                <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                  {expenseAccounts.map((account) => (
                    <SelectItem key={account.qboId} value={account.qboId}>
                      {account.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {isInventory && (
                <FormDescription className="text-xs">
                  Cost of Goods Sold account
                </FormDescription>
              )}
              <FormMessage />
            </FormItem>
          )}
        />
      )}

      {isInventory && (
        <FormField
          control={form.control}
          name="assetAccountRefValue"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Asset Account *</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger disabled={isLoadingAccounts} className="w-full">
                    <SelectValue placeholder={isLoadingAccounts ? 'Loading accounts...' : 'Select asset account'} />
                  </SelectTrigger>
                </FormControl>
                <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                  {assetAccounts.map((account) => (
                    <SelectItem key={account.qboId} value={account.qboId}>
                      {account.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormDescription className="text-xs">
                Inventory Asset account
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
      )}
    </div>
  );
}
