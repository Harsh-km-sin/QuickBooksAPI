import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type {
  QBOInvoiceHeader,
  CreateInvoiceRequest,
  UpdateInvoiceRequest,
  CreateInvoiceLineRequest,
} from '@/types';
import { Form } from '@/components/ui/form';
import { useCustomersList, useProductsList, useTermsList, useInvoice } from '@/hooks';
import { Loader2 } from 'lucide-react';
import { invoiceFormSchema, type InvoiceFormValues } from './invoiceFormSchema';
import { InvoiceFormFields } from './InvoiceFormFields';

export interface InvoiceFormProps {
  invoice?: QBOInvoiceHeader;
  onSubmit: (data: CreateInvoiceRequest | UpdateInvoiceRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

function parseInvoiceDetails(inv: QBOInvoiceHeader): InvoiceFormValues {
  let lines: InvoiceFormValues['lines'] = [];
  let docNumber = '';
  let customerMemo = inv.customerMemo || inv.privateNote || '';
  let salesTermRefId = inv.salesTermRefId || '';

  if (inv.rawJson) {
    try {
      const parsed = JSON.parse(inv.rawJson);
      docNumber = parsed.DocNumber || parsed.docNumber || '';
      if (!customerMemo && parsed.CustomerMemo?.value) {
        customerMemo = parsed.CustomerMemo.value;
      }
      if (!salesTermRefId && parsed.SalesTermRef?.value) {
        salesTermRefId = String(parsed.SalesTermRef.value);
      }

      const rawLines = parsed.Line || parsed.line;
      if (Array.isArray(rawLines)) {
        lines = rawLines
          .filter(
            (l: any) =>
              l.DetailType === 'SalesItemLineDetail' ||
              l.detailType === 'SalesItemLineDetail' ||
              l.SalesItemLineDetail ||
              l.salesItemLineDetail
          )
          .map((l: any) => {
            const detail = l.SalesItemLineDetail || l.salesItemLineDetail || {};
            const itemRef = detail.ItemRef || detail.itemRef || {};
            return {
              itemRefValue: itemRef.value ? String(itemRef.value) : '',
              itemRefName: itemRef.name || '',
              description: l.Description || l.description || '',
              qty: Number(detail.Qty ?? detail.qty ?? 1),
              unitPrice: Number(detail.UnitPrice ?? detail.unitPrice ?? 0),
              amount: Number(l.Amount ?? l.amount ?? 0),
            };
          });
      }
    } catch {
      // Fallback if rawJson parsing fails
    }
  }

  if (!lines.length) {
    lines = [
      {
        itemRefValue: '',
        itemRefName: '',
        description: '',
        qty: 1,
        unitPrice: 0,
        amount: 0,
      },
    ];
  }

  return {
    customerRefValue: inv.customerRefId || '',
    customerRefName: inv.customerRefName || '',
    txnDate: inv.txnDate ? inv.txnDate.split('T')[0] : new Date().toISOString().split('T')[0],
    dueDate: inv.dueDate ? inv.dueDate.split('T')[0] : '',
    docNumber,
    salesTermRefId,
    customerMemo,
    lines,
  };
}

export function InvoiceForm({ invoice, onSubmit, onCancel, isSubmitting }: InvoiceFormProps) {
  const { customers, isLoading: isLoadingCustomers } = useCustomersList({ pageSize: 100 });
  const { products, isLoading: isLoadingProducts } = useProductsList({ pageSize: 100 });
  const { data: terms, isLoading: isLoadingTerms } = useTermsList(true);
  const { data: detailedInvoice, isLoading: isLoadingInvoiceDetails } = useInvoice(invoice?.qboInvoiceId);

  const getTodayDateStr = () => new Date().toISOString().split('T')[0];
  const getThirtyDaysDateStr = () => {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return d.toISOString().split('T')[0];
  };

  const form = useForm<InvoiceFormValues>({
    resolver: zodResolver(invoiceFormSchema),
    defaultValues: invoice
      ? parseInvoiceDetails(invoice)
      : {
          customerRefValue: '',
          customerRefName: '',
          txnDate: getTodayDateStr(),
          dueDate: getThirtyDaysDateStr(),
          docNumber: '',
          salesTermRefId: '',
          customerMemo: '',
          lines: [
            {
              itemRefValue: '',
              itemRefName: '',
              description: '',
              qty: 1,
              unitPrice: 0,
              amount: 0,
            },
          ],
        },
    mode: 'onBlur',
  });

  useEffect(() => {
    if (detailedInvoice) {
      form.reset(parseInvoiceDetails(detailedInvoice));
    }
  }, [detailedInvoice, form]);

  const handleFormSubmit = (data: InvoiceFormValues) => {
    const lineRequests: CreateInvoiceLineRequest[] = data.lines.map((l) => ({
      detailType: 'SalesItemLineDetail',
      amount: l.amount,
      description: l.description || undefined,
      salesItemLineDetail: {
        itemRef: {
          value: l.itemRefValue,
          name: l.itemRefName || '',
        },
        qty: l.qty,
        unitPrice: l.unitPrice,
      },
    }));

    if (invoice) {
      const updateData: UpdateInvoiceRequest = {
        id: invoice.qboInvoiceId,
        syncToken: invoice.syncToken,
        customerRef: {
          value: data.customerRefValue,
          name: data.customerRefName || '',
        },
        txnDate: data.txnDate,
        dueDate: data.dueDate || undefined,
        docNumber: data.docNumber || undefined,
        salesTermRef: data.salesTermRefId ? { value: data.salesTermRefId } : undefined,
        customerMemo: data.customerMemo ? { value: data.customerMemo } : undefined,
        line: lineRequests,
      };
      onSubmit(updateData);
    } else {
      const createData: CreateInvoiceRequest = {
        customerRef: {
          value: data.customerRefValue,
          name: data.customerRefName || '',
        },
        txnDate: data.txnDate,
        dueDate: data.dueDate || undefined,
        salesTermRef: data.salesTermRefId ? { value: data.salesTermRefId } : undefined,
        customerMemo: data.customerMemo ? { value: data.customerMemo } : undefined,
        line: lineRequests,
      };
      onSubmit(createData);
    }
  };

  if (invoice && isLoadingInvoiceDetails) {
    return (
      <div className="flex flex-col items-center justify-center py-12 space-y-3">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-sm text-muted-foreground">Loading invoice details...</p>
      </div>
    );
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6">
        <InvoiceFormFields
          onCancel={onCancel}
          isSubmitting={isSubmitting}
          isEdit={!!invoice}
          customers={customers}
          products={products}
          terms={terms}
          isLoadingCustomers={isLoadingCustomers}
          isLoadingProducts={isLoadingProducts}
          isLoadingTerms={isLoadingTerms}
        />
      </form>
    </Form>
  );
}
