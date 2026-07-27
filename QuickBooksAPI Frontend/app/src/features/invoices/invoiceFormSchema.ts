import { z } from 'zod';

export const invoiceLineSchema = z.object({
  itemRefValue: z.string().min(1, 'Product/Service is required'),
  itemRefName: z.string().optional(),
  description: z.string().max(4000, 'Description must be 4000 characters or less').optional().or(z.literal('')),
  qty: z.number().min(0.01, 'Quantity must be greater than 0'),
  unitPrice: z.number().min(0, 'Unit price must be 0 or greater'),
  amount: z.number().min(0, 'Amount must be 0 or greater'),
});

const dateRegex = /^\d{4}-\d{2}-\d{2}$/;

export const invoiceFormSchema = z
  .object({
    customerRefValue: z.string().min(1, 'Customer is required'),
    customerRefName: z.string().optional(),
    txnDate: z
      .string()
      .min(1, 'Transaction date is required')
      .regex(dateRegex, 'Transaction date must be in YYYY-MM-DD format'),
    dueDate: z
      .string()
      .optional()
      .or(z.literal(''))
      .refine((val) => !val || dateRegex.test(val), {
        message: 'Due date must be in YYYY-MM-DD format',
      }),
    docNumber: z.string().max(50, 'Doc number must be 50 characters or less').optional().or(z.literal('')),
    salesTermRefId: z.string().optional().or(z.literal('')),
    customerMemo: z.string().optional().or(z.literal('')),
    privateNote: z.string().optional().or(z.literal('')),
    lines: z.array(invoiceLineSchema).min(1, 'At least one line item is required'),
  })
  .refine(
    (data) => {
      if (!data.dueDate || !data.txnDate) return true;
      return data.dueDate >= data.txnDate;
    },
    {
      message: 'Due date cannot be earlier than transaction date',
      path: ['dueDate'],
    }
  );

export type InvoiceFormValues = z.infer<typeof invoiceFormSchema>;
export type InvoiceLineValues = z.infer<typeof invoiceLineSchema>;
