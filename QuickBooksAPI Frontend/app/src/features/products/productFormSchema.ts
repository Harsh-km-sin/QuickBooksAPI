import { z } from 'zod';

export const productFormSchema = z.object({
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

export type ProductFormValues = z.infer<typeof productFormSchema>;
