import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { Products, CreateProductRequest, UpdateProductRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Checkbox } from '@/components/ui/checkbox';
import { DialogFooter } from '@/components/ui/dialog';
import {
  Form,
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
import { Loader2, Package, DollarSign, Warehouse, BookOpen } from 'lucide-react';
import { useChartOfAccounts } from '@/hooks';

const productFormSchema = z.object({
  name: z
    .string()
    .min(1, 'Product name is required')
    .max(100, 'Name must be 100 characters or less'),
  description: z.string().max(4000, 'Description must be 4000 characters or less').optional().or(z.literal('')),
  active: z.boolean().optional(),
  trackQtyOnHand: z.boolean().optional(),
  type: z.string().min(1, 'Please select a product type'),
  incomeAccountRefValue: z.string().optional().or(z.literal('')),
  expenseAccountRefValue: z.string().optional().or(z.literal('')),
  assetAccountRefValue: z.string().optional().or(z.literal('')),
  unitPrice: z.string().optional().or(z.literal('')),
  purchaseCost: z.string().optional().or(z.literal('')),
  qtyOnHand: z.string().optional().or(z.literal('')),
  invStartDate: z.string().optional().or(z.literal('')),
});

type ProductFormValues = z.infer<typeof productFormSchema>;

export interface ProductFormProps {
  product?: Products;
  onSubmit: (data: CreateProductRequest | UpdateProductRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function ProductForm({ product, onSubmit, onCancel, isSubmitting }: ProductFormProps) {
  const [iSell, setISell] = useState(product ? !!product.incomeAccountRefValue : true);
  const [iPurchase, setIPurchase] = useState(product ? !!product.expenseAccountRefValue : false);

  const { accounts, isLoading: isLoadingAccounts } = useChartOfAccounts({ 
    listParams: { page: 1, pageSize: 500 } 
  });

  const incomeAccounts = accounts.filter(acc => {
    const type = acc.accountType?.toLowerCase();
    return type === 'income' || type === 'other income';
  });
  
  const expenseAccounts = accounts.filter(acc => {
    const type = acc.accountType?.toLowerCase();
    return type === 'expense' || type === 'other expense' || type === 'cost of goods sold';
  });
  
  const assetAccounts = accounts.filter(acc => 
    acc.accountType?.toLowerCase() === 'other current asset' &&
    acc.accountSubType?.toLowerCase() === 'inventory'
  );

  const form = useForm<ProductFormValues>({
    resolver: zodResolver(productFormSchema),
    defaultValues: {
      name: product?.name || '',
      description: product?.description || '',
      active: product?.active ?? true,
      trackQtyOnHand: product?.trackQtyOnHand ?? false,
      type: product?.type || 'Service',
      incomeAccountRefValue: product?.incomeAccountRefValue || '',
      expenseAccountRefValue: product?.expenseAccountRefValue || '',
      assetAccountRefValue: product?.assetAccountRefValue || '',
      unitPrice: product?.unitPrice?.toString() || '0',
      purchaseCost: product?.purchaseCost?.toString() || '0',
      qtyOnHand: product?.qtyOnHand?.toString() || '0',
      invStartDate: product?.invStartDate || '',
    },
    mode: 'onBlur',
  });

  const productType = form.watch('type');
  const isInventory = productType === 'Inventory';
  const isServiceOrNonInventory = productType === 'Service' || productType === 'NonInventory';

  const getAccountName = (accountList: typeof accounts, value: string) => {
    return accountList.find(acc => acc.qboId === value)?.name || '';
  };

  const handleSellChange = (checked: boolean) => {
    if (!checked && !iPurchase) return;
    setISell(checked);
    if (!checked) {
      form.setValue('incomeAccountRefValue', '');
    }
  };

  const handlePurchaseChange = (checked: boolean) => {
    if (!checked && !iSell) return;
    setIPurchase(checked);
    if (!checked) {
      form.setValue('expenseAccountRefValue', '');
    }
  };

  const handleFormSubmit = (data: ProductFormValues) => {
    const errors: string[] = [];

    if (isInventory) {
      if (!data.incomeAccountRefValue) {
        errors.push('Income Account is required');
      }
      if (!data.expenseAccountRefValue) {
        errors.push('Expense Account is required for Inventory products');
      }
      if (!data.assetAccountRefValue) {
        errors.push('Asset Account is required for Inventory products');
      }
    } else {
      if (iSell && !data.incomeAccountRefValue) {
        errors.push('Income Account is required');
      }
      if (iPurchase && !data.expenseAccountRefValue) {
        errors.push('Expense Account is required');
      }
    }

    if (errors.length > 0) {
      errors.forEach(err => {
        form.setError('root', { message: err });
      });
      return;
    }

    const incomeAccountRef = data.incomeAccountRefValue 
      ? { value: data.incomeAccountRefValue, name: getAccountName(incomeAccounts, data.incomeAccountRefValue) }
      : undefined;
    
    const expenseAccountRef = data.expenseAccountRefValue 
      ? { value: data.expenseAccountRefValue, name: getAccountName(expenseAccounts, data.expenseAccountRefValue) }
      : undefined;
    
    const assetAccountRef = data.assetAccountRefValue 
      ? { value: data.assetAccountRefValue, name: getAccountName(assetAccounts, data.assetAccountRefValue) }
      : undefined;

    const cleanedData = {
      name: data.name,
      description: data.description || undefined,
      active: data.active ?? true,
      trackQtyOnHand: data.type === 'Inventory' ? true : (data.trackQtyOnHand ?? false),
      type: data.type,
      incomeAccountRef,
      expenseAccountRef,
      assetAccountRef,
      unitPrice: parseFloat(data.unitPrice || '0') || 0,
      purchaseCost: parseFloat(data.purchaseCost || '0') || 0,
      qtyOnHand: parseInt(data.qtyOnHand || '0') || 0,
      invStartDate: data.invStartDate || undefined,
    };

    if (product) {
      onSubmit({
        id: product.qboId,
        syncToken: product.syncToken,
        ...cleanedData,
      } as UpdateProductRequest);
    } else {
      onSubmit(cleanedData as CreateProductRequest);
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6">
        {/* Basic Information Section */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <Package className="h-4 w-4" />
            Basic Information
          </div>

          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Product Name *</FormLabel>
                <FormControl>
                  <Input placeholder="Product or Service Name" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="description"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Description</FormLabel>
                <FormControl>
                  <Textarea
                    placeholder="Product description..."
                    className="min-h-[80px] resize-none"
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="type"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Product Type *</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="Service">Service</SelectItem>
                      <SelectItem value="Inventory">Inventory</SelectItem>
                      <SelectItem value="NonInventory">Non-Inventory</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="active"
              render={({ field }) => (
                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                  <div className="space-y-0.5">
                    <FormLabel>Active</FormLabel>
                    <FormDescription className="text-xs">
                      Product is available for use
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Pricing Section */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <DollarSign className="h-4 w-4" />
            Pricing
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="unitPrice"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Sales Price / Rate</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="purchaseCost"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Purchase Cost</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Inventory Section - Only for Inventory type */}
        {isInventory && (
          <div className="space-y-4">
            <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
              <Warehouse className="h-4 w-4" />
              Inventory Settings
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="qtyOnHand"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Quantity On Hand</FormLabel>
                    <FormControl>
                      <Input
                        type="number"
                        min="0"
                        placeholder="0"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="invStartDate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Inventory Start Date</FormLabel>
                    <FormControl>
                      <Input
                        type="date"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
          </div>
        )}

        {/* Account References Section */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <BookOpen className="h-4 w-4" />
            Account References
          </div>

          {/* Checkboxes for Service / NonInventory */}
          {isServiceOrNonInventory && (
            <div className="space-y-3">
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="iSell"
                  checked={iSell}
                  onCheckedChange={(checked) => handleSellChange(checked === true)}
                />
                <label htmlFor="iSell" className="text-sm font-medium leading-none cursor-pointer">
                  I sell this product/service to my customers.
                </label>
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="iPurchase"
                  checked={iPurchase}
                  onCheckedChange={(checked) => handlePurchaseChange(checked === true)}
                />
                <label htmlFor="iPurchase" className="text-sm font-medium leading-none cursor-pointer">
                  I purchase this product/service from a supplier.
                </label>
              </div>
            </div>
          )}

          {/* Income Account - shown if "I sell" is checked (Service/NonInventory) or always for Inventory */}
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
                        <SelectValue placeholder={isLoadingAccounts ? "Loading accounts..." : "Select income account"} />
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

          {/* Expense Account - shown if "I purchase" is checked (Service/NonInventory) or always for Inventory */}
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
                        <SelectValue placeholder={isLoadingAccounts ? "Loading accounts..." : "Select expense account"} />
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

          {/* Asset Account - Inventory only */}
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
                        <SelectValue placeholder={isLoadingAccounts ? "Loading accounts..." : "Select asset account"} />
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

        {form.formState.errors.root && (
          <p className="text-sm text-destructive">{form.formState.errors.root.message}</p>
        )}

        <DialogFooter className="pt-4 border-t">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting || isLoadingAccounts}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {product ? 'Update' : 'Create'} Product
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
}
