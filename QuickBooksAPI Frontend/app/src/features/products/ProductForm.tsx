import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { Products, CreateProductRequest, UpdateProductRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { DialogFooter } from '@/components/ui/dialog';
import { Form } from '@/components/ui/form';
import { Loader2 } from 'lucide-react';
import { useChartOfAccountsList } from '@/hooks';
import { productFormSchema, type ProductFormValues } from './productFormSchema';
import { ProductFormBasicPricing } from './ProductFormBasicPricing';
import { ProductFormAccountRefs } from './ProductFormAccountRefs';

export interface ProductFormProps {
  product?: Products;
  onSubmit: (data: CreateProductRequest | UpdateProductRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function ProductForm({ product, onSubmit, onCancel, isSubmitting }: ProductFormProps) {
  const [iSell, setISell] = useState(product ? !!product.incomeAccountRefValue : true);
  const [iPurchase, setIPurchase] = useState(product ? !!product.expenseAccountRefValue : false);

  const { accounts, isLoading: isLoadingAccounts } = useChartOfAccountsList({ page: 1, pageSize: 500 });

  const incomeAccounts = accounts.filter((acc) => {
    const type = acc.accountType?.toLowerCase();
    return type === 'income' || type === 'other income';
  });

  const expenseAccounts = accounts.filter((acc) => {
    const type = acc.accountType?.toLowerCase();
    return type === 'expense' || type === 'other expense' || type === 'cost of goods sold';
  });

  const assetAccounts = accounts.filter(
    (acc) =>
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

  const getAccountName = (accountList: typeof accounts, value: string) =>
    accountList.find((acc) => acc.qboId === value)?.name || '';

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
      errors.forEach((err) => {
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
      qtyOnHand: parseInt(data.qtyOnHand || '0', 10) || 0,
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
        <ProductFormBasicPricing />
        <ProductFormAccountRefs
          iSell={iSell}
          iPurchase={iPurchase}
          onSellChange={handleSellChange}
          onPurchaseChange={handlePurchaseChange}
          incomeAccounts={incomeAccounts}
          expenseAccounts={expenseAccounts}
          assetAccounts={assetAccounts}
          isLoadingAccounts={isLoadingAccounts}
        />

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
