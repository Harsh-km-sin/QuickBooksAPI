import { z } from 'zod';

export const customerFormSchema = z.object({
  title: z.string().max(16, 'Title must be 16 characters or less').optional().or(z.literal('')),
  givenName: z
    .string()
    .min(1, 'First name is required')
    .max(50, 'First name must be 50 characters or less'),
  middleName: z.string().max(50, 'Middle name must be 50 characters or less').optional().or(z.literal('')),
  familyName: z
    .string()
    .min(1, 'Last name is required')
    .max(50, 'Last name must be 50 characters or less'),
  suffix: z.string().max(16, 'Suffix must be 16 characters or less').optional().or(z.literal('')),
  displayName: z
    .string()
    .min(1, 'Display name is required')
    .max(100, 'Display name must be 100 characters or less'),
  fullyQualifiedName: z.string().max(100, 'Fully qualified name must be 100 characters or less').optional().or(z.literal('')),
  companyName: z.string().max(100, 'Company name must be 100 characters or less').optional().or(z.literal('')),
  notes: z.string().max(4000, 'Notes must be 4000 characters or less').optional().or(z.literal('')),
  primaryEmailAddr: z.object({
    address: z
      .string()
      .email('Please enter a valid email address')
      .max(100, 'Email must be 100 characters or less')
      .optional()
      .or(z.literal('')),
  }).optional(),
  primaryPhone: z.object({
    freeFormNumber: z
      .string()
      .max(15, 'Phone number must be 15 characters or less')
      .optional()
      .or(z.literal('')),
  }).optional(),
  billAddr: z.object({
    line1: z.string().max(500, 'Street address must be 500 characters or less').optional().or(z.literal('')),
    city: z.string().max(255, 'City must be 255 characters or less').optional().or(z.literal('')),
    countrySubDivisionCode: z.string().max(255, 'State/Province must be 255 characters or less').optional().or(z.literal('')),
    postalCode: z.string().max(30, 'Postal code must be 30 characters or less').optional().or(z.literal('')),
    country: z.string().max(255, 'Country must be 255 characters or less').optional().or(z.literal('')),
  }).optional(),
});

export type CustomerFormValues = z.infer<typeof customerFormSchema>;
