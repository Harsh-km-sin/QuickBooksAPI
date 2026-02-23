import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { Vendor, CreateVendorRequest, UpdateVendorRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { DialogFooter } from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Loader2, User, Building2, Phone, MapPin, FileText } from 'lucide-react';

const vendorFormSchema = z.object({
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

type VendorFormValues = z.infer<typeof vendorFormSchema>;

export interface VendorFormProps {
  vendor?: Vendor;
  onSubmit: (data: CreateVendorRequest | UpdateVendorRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function VendorForm({ vendor, onSubmit, onCancel, isSubmitting }: VendorFormProps) {
  const form = useForm<VendorFormValues>({
    resolver: zodResolver(vendorFormSchema),
    defaultValues: {
      title: vendor?.title || '',
      givenName: vendor?.givenName || '',
      middleName: vendor?.middleName || '',
      familyName: vendor?.familyName || '',
      suffix: vendor?.suffix || '',
      displayName: vendor?.displayName || '',
      companyName: vendor?.companyName || '',
      primaryEmailAddr: { address: vendor?.primaryEmailAddr || '' },
      primaryPhone: { freeFormNumber: vendor?.primaryPhone || '' },
      mobile: { freeFormNumber: vendor?.mobile || '' },
      webAddr: { uri: vendor?.webAddr || '' },
      billAddr: {
        line1: vendor?.billAddrLine1 || '',
        line2: vendor?.billAddrLine2 || '',
        line3: vendor?.billAddrLine3 || '',
        city: vendor?.billAddrCity || '',
        countrySubDivisionCode: vendor?.billAddrCountrySubDivisionCode || '',
        postalCode: vendor?.billAddrPostalCode || '',
        country: vendor?.billAddrCountry || '',
      },
      printOnCheckName: vendor?.printOnCheckName || '',
      acctNum: vendor?.acctNum || '',
      taxIdentifier: vendor?.taxIdentifier || '',
    },
    mode: 'onBlur',
  });

  const handleFormSubmit = (data: VendorFormValues) => {
    if (vendor) {
      onSubmit({
        id: vendor.qboId,
        syncToken: vendor.syncToken,
        ...data,
      } as UpdateVendorRequest);
    } else {
      onSubmit(data as CreateVendorRequest);
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6">
        {/* Personal Information */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <User className="h-4 w-4" />
            Personal Information
          </div>

          <div className="grid grid-cols-5 gap-3">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title</FormLabel>
                  <FormControl>
                    <Input placeholder="Mr./Ms." {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="givenName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input placeholder="John" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="middleName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Middle Name</FormLabel>
                  <FormControl>
                    <Input placeholder="M." {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="familyName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Last Name</FormLabel>
                  <FormControl>
                    <Input placeholder="Doe" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="suffix"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Suffix</FormLabel>
                  <FormControl>
                    <Input placeholder="Jr./Sr." {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Display & Company Information */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <Building2 className="h-4 w-4" />
            Display & Company Information
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="displayName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Display Name *</FormLabel>
                  <FormControl>
                    <Input placeholder="John Doe" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="companyName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Company Name</FormLabel>
                  <FormControl>
                    <Input placeholder="Acme Corporation" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <FormField
            control={form.control}
            name="printOnCheckName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Print on Check Name</FormLabel>
                <FormControl>
                  <Input placeholder="Name to print on checks" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Contact Information */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <Phone className="h-4 w-4" />
            Contact Information
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="primaryEmailAddr.address"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email Address</FormLabel>
                  <FormControl>
                    <Input type="email" placeholder="vendor@example.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="webAddr.uri"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Website</FormLabel>
                  <FormControl>
                    <Input placeholder="https://example.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="primaryPhone.freeFormNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Phone Number</FormLabel>
                  <FormControl>
                    <Input placeholder="(555) 123-4567" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="mobile.freeFormNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Mobile Number</FormLabel>
                  <FormControl>
                    <Input placeholder="(555) 987-6543" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Billing Address */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <MapPin className="h-4 w-4" />
            Billing Address
          </div>

          <FormField
            control={form.control}
            name="billAddr.line1"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Street Address</FormLabel>
                <FormControl>
                  <Input placeholder="123 Main Street" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="billAddr.line2"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Address Line 2</FormLabel>
                  <FormControl>
                    <Input placeholder="Suite 100" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="billAddr.line3"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Address Line 3</FormLabel>
                  <FormControl>
                    <Input placeholder="Building A" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="billAddr.city"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>City</FormLabel>
                  <FormControl>
                    <Input placeholder="New York" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="billAddr.countrySubDivisionCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>State / Province</FormLabel>
                  <FormControl>
                    <Input placeholder="NY" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="billAddr.postalCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Postal Code</FormLabel>
                  <FormControl>
                    <Input placeholder="10001" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="billAddr.country"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Country</FormLabel>
                  <FormControl>
                    <Input placeholder="USA" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        {/* Additional Details */}
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground border-b pb-2">
            <FileText className="h-4 w-4" />
            Additional Details
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField
              control={form.control}
              name="acctNum"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Account Number</FormLabel>
                  <FormControl>
                    <Input placeholder="Vendor account number" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="taxIdentifier"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tax ID</FormLabel>
                  <FormControl>
                    <Input placeholder="Tax identifier" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        <DialogFooter className="pt-4 border-t">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting || !form.formState.isValid}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {vendor ? 'Update' : 'Create'} Vendor
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
}
