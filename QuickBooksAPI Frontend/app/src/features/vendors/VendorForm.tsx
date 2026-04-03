import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { Vendor, CreateVendorRequest, UpdateVendorRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { DialogFooter } from '@/components/ui/dialog';
import { Form } from '@/components/ui/form';
import { Loader2 } from 'lucide-react';
import { vendorFormSchema, type VendorFormValues } from './vendorFormSchema';
import { VendorFormIdentity } from './VendorFormIdentity';
import { VendorFormContactAndMore } from './VendorFormContactAndMore';

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
        <VendorFormIdentity />
        <VendorFormContactAndMore />
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
