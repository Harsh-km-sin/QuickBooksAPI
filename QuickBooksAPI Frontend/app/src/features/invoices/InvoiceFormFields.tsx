import { useFormContext, useFieldArray } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { DialogFooter } from '@/components/ui/dialog';
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DatePicker, parseApiDate } from '@/components/ui/date-picker';
import { Loader2, User, Receipt, Plus, Trash2, FileText, DollarSign } from 'lucide-react';
import { useRef } from 'react';
import type { Customer, Products } from '@/types';
import type { QBOTerm } from '@/types/term';
import type { InvoiceFormValues } from './invoiceFormSchema';

export interface InvoiceFormFieldsProps {
  onCancel: () => void;
  isSubmitting: boolean;
  isEdit: boolean;
  customers: Customer[];
  products: Products[];
  terms?: QBOTerm[];
  isLoadingCustomers: boolean;
  isLoadingProducts: boolean;
  isLoadingTerms?: boolean;
}

export function InvoiceFormFields({
  onCancel,
  isSubmitting,
  isEdit,
  customers,
  products,
  terms = [],
  isLoadingCustomers,
  isLoadingProducts,
  isLoadingTerms = false,
}: InvoiceFormFieldsProps) {
  const form = useFormContext<InvoiceFormValues>();

  const isDueDateDirtyRef = useRef<boolean>(false);

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'lines',
  });

  const lines = form.watch('lines') || [];
  const txnDateStr = form.watch('txnDate');
  const txnDateObj = parseApiDate(txnDateStr);
  const totalAmount = lines.reduce((sum, line) => sum + (Number(line?.amount) || 0), 0);

  const calculateDueDate = (baseTxnDate: string, dueDays: number) => {
    if (!baseTxnDate) return '';
    const date = new Date(baseTxnDate);
    if (isNaN(date.getTime())) return '';
    date.setDate(date.getDate() + dueDays);
    return date.toISOString().split('T')[0];
  };

  const handleTermChange = (termId: string) => {
    form.setValue('salesTermRefId', termId, { shouldValidate: true });
    const selectedTerm = terms.find((t) => t.qboTermId === termId);
    if (selectedTerm && typeof selectedTerm.dueDays === 'number') {
      const calculated = calculateDueDate(form.getValues('txnDate'), selectedTerm.dueDays);
      if (calculated) {
        isDueDateDirtyRef.current = false;
        form.setValue('dueDate', calculated, { shouldValidate: true });
      }
    }
  };

  const handleTxnDateChange = (val: string) => {
    form.setValue('txnDate', val, { shouldValidate: true });
    form.trigger('dueDate');

    if (!isDueDateDirtyRef.current) {
      const currentTermId = form.getValues('salesTermRefId');
      if (currentTermId) {
        const selectedTerm = terms.find((t) => t.qboTermId === currentTermId);
        if (selectedTerm && typeof selectedTerm.dueDays === 'number') {
          const calculated = calculateDueDate(val, selectedTerm.dueDays);
          if (calculated) {
            form.setValue('dueDate', calculated, { shouldValidate: true });
          }
        }
      }
    }
  };

  const handleDueDateChange = (val: string) => {
    isDueDateDirtyRef.current = true;
    form.setValue('dueDate', val, { shouldValidate: true });
  };

  const handleProductChange = (index: number, productId: string) => {
    const selectedProduct = products.find((p) => p.qboId === productId);
    if (!selectedProduct) return;

    form.setValue(`lines.${index}.itemRefValue`, selectedProduct.qboId, { shouldValidate: true });
    form.setValue(`lines.${index}.itemRefName`, selectedProduct.name);
    
    if (selectedProduct.description) {
      form.setValue(`lines.${index}.description`, selectedProduct.description);
    }
    
    const unitPrice = selectedProduct.unitPrice ?? 0;
    form.setValue(`lines.${index}.unitPrice`, unitPrice, { shouldValidate: true });

    const qty = form.getValues(`lines.${index}.qty`) || 1;
    form.setValue(`lines.${index}.amount`, Number((qty * unitPrice).toFixed(2)), { shouldValidate: true });
  };

  const handleQtyChange = (index: number, qtyVal: number) => {
    const qty = qtyVal;
    const unitPrice = Number(form.getValues(`lines.${index}.unitPrice`)) || 0;
    form.setValue(`lines.${index}.qty`, qty, { shouldValidate: true });
    form.setValue(`lines.${index}.amount`, Number((qty * unitPrice).toFixed(2)), { shouldValidate: true });
  };

  const handleUnitPriceChange = (index: number, priceVal: number) => {
    const unitPrice = priceVal;
    const qty = Number(form.getValues(`lines.${index}.qty`)) || 0;
    form.setValue(`lines.${index}.unitPrice`, unitPrice, { shouldValidate: true });
    form.setValue(`lines.${index}.amount`, Number((qty * unitPrice).toFixed(2)), { shouldValidate: true });
  };

  const handleCustomerChange = (customerId: string) => {
    const customer = customers.find((c) => c.qboId === customerId);
    if (customer) {
      form.setValue('customerRefValue', customer.qboId, { shouldValidate: true });
      form.setValue('customerRefName', customer.displayName || `${customer.givenName} ${customer.familyName}`);
    }
  };

  return (
    <>
      <div className="space-y-5 max-h-[68vh] overflow-y-auto pr-2 pb-2">
        {/* Customer & Transaction Information */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <User className="h-4 w-4" />
            Customer & Document Info
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="customerRefValue"
              render={({ field }) => (
                <FormItem className="min-w-0">
                  <FormLabel>Customer *</FormLabel>
                  <Select
                    onValueChange={(val) => {
                      field.onChange(val);
                      handleCustomerChange(val);
                    }}
                    value={field.value}
                  >
                    <FormControl>
                      <SelectTrigger disabled={isLoadingCustomers} className="w-full">
                        <SelectValue placeholder={isLoadingCustomers ? 'Loading customers...' : 'Select customer'} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                      {customers.map((c) => (
                        <SelectItem key={c.qboId} value={c.qboId}>
                          {c.displayName || `${c.givenName} ${c.familyName}`}
                          {c.companyName ? ` (${c.companyName})` : ''}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="salesTermRefId"
              render={({ field }) => (
                <FormItem className="min-w-0">
                  <FormLabel>Payment Terms</FormLabel>
                  <Select
                    onValueChange={(val) => {
                      field.onChange(val);
                      handleTermChange(val);
                    }}
                    value={field.value || ''}
                  >
                    <FormControl>
                      <SelectTrigger disabled={isLoadingTerms} className="w-full">
                        <SelectValue placeholder={isLoadingTerms ? 'Loading terms...' : 'Select payment term'} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                      {terms.map((t) => (
                        <SelectItem key={t.qboTermId} value={t.qboTermId}>
                          {t.name} {typeof t.dueDays === 'number' ? `(${t.dueDays} days)` : ''}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <FormField
              control={form.control}
              name="txnDate"
              render={({ field }) => (
                <FormItem className="flex flex-col">
                  <FormLabel>Transaction Date *</FormLabel>
                  <FormControl>
                    <DatePicker
                      id="txnDate"
                      value={field.value}
                      onChange={(val) => handleTxnDateChange(val)}
                      placeholder="Select transaction date"
                      className="w-full sm:w-full"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="dueDate"
              render={({ field }) => (
                <FormItem className="flex flex-col">
                  <FormLabel>Due Date</FormLabel>
                  <FormControl>
                    <DatePicker
                      id="dueDate"
                      value={field.value}
                      onChange={(val) => handleDueDateChange(val)}
                      placeholder="Select due date"
                      fromDate={txnDateObj}
                      className="w-full sm:w-full"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isEdit && (
              <FormField
                control={form.control}
                name="docNumber"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Invoice # / Doc Number</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. 1001" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}
          </div>
        </div>

        {/* Invoice Line Items */}
        <div className="space-y-4">
          <div className="flex items-center justify-between border-b pb-2">
            <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
              <Receipt className="h-4 w-4" />
              Line Items
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() =>
                append({
                  itemRefValue: '',
                  itemRefName: '',
                  description: '',
                  qty: 1,
                  unitPrice: 0,
                  amount: 0,
                })
              }
            >
              <Plus className="h-4 w-4 mr-1" />
              Add Line Item
            </Button>
          </div>

          {fields.map((fieldItem, index) => (
            <div
              key={fieldItem.id}
              className="p-4 rounded-lg border bg-card/50 space-y-3 relative group transition-colors hover:border-accent"
            >
              <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-start">
                <div className="md:col-span-4 min-w-0">
                  <FormField
                    control={form.control}
                    name={`lines.${index}.itemRefValue`}
                    render={({ field }) => (
                      <FormItem className="min-w-0">
                        <FormLabel className="text-xs">Product / Service *</FormLabel>
                        <Select
                          onValueChange={(val) => {
                            field.onChange(val);
                            handleProductChange(index, val);
                          }}
                          value={field.value}
                        >
                          <FormControl>
                            <SelectTrigger disabled={isLoadingProducts} className="w-full h-9 text-sm">
                              <SelectValue placeholder={isLoadingProducts ? 'Loading...' : 'Select item'} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent position="popper" className="max-h-60 w-[var(--radix-select-trigger-width)]">
                            {products.map((p) => (
                              <SelectItem key={p.qboId} value={p.qboId}>
                                {p.name} ({new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(p.unitPrice ?? 0)})
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <div className="md:col-span-4 min-w-0">
                  <FormField
                    control={form.control}
                    name={`lines.${index}.description`}
                    render={({ field }) => (
                      <FormItem className="min-w-0">
                        <FormLabel className="text-xs">Description</FormLabel>
                        <FormControl>
                          <Input placeholder="Line description..." className="h-9 text-sm" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <div className="md:col-span-1 min-w-0">
                  <FormField
                    control={form.control}
                    name={`lines.${index}.qty`}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="text-xs">Qty *</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            step="any"
                            min="0.01"
                            className="h-9 text-sm px-2"
                            value={field.value ?? ''}
                            onChange={(e) => {
                              const val = parseFloat(e.target.value) || 0;
                              field.onChange(val);
                              handleQtyChange(index, val);
                            }}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <div className="md:col-span-2 min-w-0">
                  <FormField
                    control={form.control}
                    name={`lines.${index}.unitPrice`}
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="text-xs">Price ($) *</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            step="any"
                            min="0"
                            className="h-9 text-sm px-2"
                            value={field.value ?? ''}
                            onChange={(e) => {
                              const val = parseFloat(e.target.value) || 0;
                              field.onChange(val);
                              handleUnitPriceChange(index, val);
                            }}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <div className="md:col-span-1 flex items-center justify-end pt-6">
                  {fields.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive hover:text-destructive/80"
                      onClick={() => remove(index)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>

              <div className="flex justify-between items-center text-xs text-muted-foreground pt-1 border-t border-border/50">
                <span>Line Amount:</span>
                <span className="font-semibold text-foreground">
                  {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(
                    lines[index]?.amount || 0
                  )}
                </span>
              </div>
            </div>
          ))}

          {form.formState.errors.lines?.root && (
            <p className="text-xs font-medium text-destructive">
              {form.formState.errors.lines.root.message}
            </p>
          )}
        </div>

        {/* Total & Customer Memo */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <FileText className="h-4 w-4" />
            Notes & Summary
          </div>

          <div className="grid grid-cols-1 md:grid-cols-5 gap-4 items-start pb-1">
            <div className="md:col-span-3 min-w-0">
              <FormField
                control={form.control}
                name="customerMemo"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Customer Memo</FormLabel>
                    <FormControl>
                      <Textarea
                        placeholder="Memo printed on invoice / sent to customer..."
                        className="min-h-[80px] resize-none"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="md:col-span-2 min-w-0 bg-muted/40 p-4 rounded-lg border space-y-2">
              <div className="text-xs text-muted-foreground font-medium uppercase tracking-wider">
                Total Invoice Amount
              </div>
              <div className="flex items-center text-2xl font-bold text-primary truncate">
                <DollarSign className="h-6 w-6 mr-1 shrink-0" />
                <span className="truncate">
                  {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
                    .format(totalAmount)
                    .replace('$', '')}
                </span>
              </div>
              <div className="text-xs text-muted-foreground">
                {lines.length} line item{lines.length !== 1 ? 's' : ''}
              </div>
            </div>
          </div>
        </div>
      </div>

      <DialogFooter className="pt-4 border-t">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isEdit ? 'Update Invoice' : 'Create Invoice'}
        </Button>
      </DialogFooter>
    </>
  );
}
