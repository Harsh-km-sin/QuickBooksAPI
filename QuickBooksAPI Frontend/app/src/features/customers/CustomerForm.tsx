import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { DialogFooter } from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Loader2, User, Building2, Phone, MapPin } from 'lucide-react';

const customerFormSchema = z.object({
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

type CustomerFormValues = z.infer<typeof customerFormSchema>;

export interface CustomerFormProps {
  customer?: Customer;
  onSubmit: (data: CreateCustomerRequest | UpdateCustomerRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function CustomerForm({ customer, onSubmit, onCancel, isSubmitting }: CustomerFormProps) {
  const form = useForm<CustomerFormValues>({
    resolver: zodResolver(customerFormSchema),
    defaultValues: {
      title: customer?.title || '',
      givenName: customer?.givenName || '',
      middleName: customer?.middleName || '',
      familyName: customer?.familyName || '',
      suffix: '',
      displayName: customer?.displayName || '',
      fullyQualifiedName: '',
      companyName: customer?.companyName || '',
      notes: '',
      primaryEmailAddr: { address: customer?.primaryEmailAddr || '' },
      primaryPhone: { freeFormNumber: customer?.primaryPhone || '' },
      billAddr: {
        line1: customer?.billAddrLine1 || '',
        city: customer?.billAddrCity || '',
        countrySubDivisionCode: customer?.billAddrCountrySubDivisionCode || '',
        postalCode: customer?.billAddrPostalCode || '',
        country: '',
      },
    },
    mode: 'onBlur',
  });

  const handleFormSubmit = (data: CustomerFormValues) => {
    if (customer) {
      onSubmit({
        id: customer.qboId,
        syncToken: customer.syncToken,
        ...data,
      } as UpdateCustomerRequest);
    } else {
      onSubmit(data as CreateCustomerRequest);
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6 max-h-[70vh] overflow-y-auto pr-2">
        {/* Personal Information Section */}
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
                  <FormLabel>First Name *</FormLabel>
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
                    <Input placeholder="William" {...field} />
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
                  <FormLabel>Last Name *</FormLabel>
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

        {/* Display & Company Information Section */}
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
              name="fullyQualifiedName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Fully Qualified Name</FormLabel>
                  <FormControl>
                    <Input placeholder="Parent:Child" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

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

        {/* Contact Information Section */}
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
                    <Input type="email" placeholder="john.doe@example.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
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
          </div>
        </div>

        {/* Billing Address Section */}
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

        {/* Notes Section */}
        <FormField
          control={form.control}
          name="notes"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Notes</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Additional notes about this customer..."
                  className="min-h-[80px] resize-none"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <DialogFooter className="pt-4 border-t">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting || !form.formState.isValid}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {customer ? 'Update' : 'Create'} Customer
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
}
