import { z } from 'zod';

export const vendorFormSchema = z.object({
  title: z.string().max(50, 'Title must be 50 characters or less').optional().or(z.literal('')),
  givenName: z.string().max(100, 'First name must be 100 characters or less').optional().or(z.literal('')),
  middleName: z.string().max(100, 'Middle name must be 100 characters or less').optional().or(z.literal('')),
  familyName: z.string().max(100, 'Last name must be 100 characters or less').optional().or(z.literal('')),
  suffix: z.string().max(50, 'Suffix must be 50 characters or less').optional().or(z.literal('')),
  displayName: z
    .string()
    .min(1, 'Display name is required')
    .max(500, 'Display name must be 500 characters or less'),
  companyName: z.string().max(500, 'Company name must be 500 characters or less').optional().or(z.literal('')),
  primaryEmailAddr: z.object({
    address: z
      .string()
      .email('Please enter a valid email address')
      .max(100, 'Email must be 100 characters or less')
      .optional()
      .or(z.literal('')),
  }).optional(),
  primaryPhone: z.object({
    freeFormNumber: z.string().max(50, 'Phone must be 50 characters or less').optional().or(z.literal('')),
  }).optional(),
  mobile: z.object({
    freeFormNumber: z.string().max(50, 'Mobile must be 50 characters or less').optional().or(z.literal('')),
  }).optional(),
  webAddr: z.object({
    uri: z.string().max(500, 'Website must be 500 characters or less').optional().or(z.literal('')),
  }).optional(),
  billAddr: z.object({
    line1: z.string().max(500, 'Address line 1 must be 500 characters or less').optional().or(z.literal('')),
    line2: z.string().max(500, 'Address line 2 must be 500 characters or less').optional().or(z.literal('')),
    line3: z.string().max(500, 'Address line 3 must be 500 characters or less').optional().or(z.literal('')),
    city: z.string().max(255, 'City must be 255 characters or less').optional().or(z.literal('')),
    countrySubDivisionCode: z.string().max(255, 'State must be 255 characters or less').optional().or(z.literal('')),
    postalCode: z.string().max(30, 'Postal code must be 30 characters or less').optional().or(z.literal('')),
    country: z.string().max(255, 'Country must be 255 characters or less').optional().or(z.literal('')),
  }).optional(),
  printOnCheckName: z.string().max(500, 'Print on check name must be 500 characters or less').optional().or(z.literal('')),
  acctNum: z.string().max(100, 'Account number must be 100 characters or less').optional().or(z.literal('')),
  taxIdentifier: z.string().max(50, 'Tax ID must be 50 characters or less').optional().or(z.literal('')),
});

export type VendorFormValues = z.infer<typeof vendorFormSchema>;
