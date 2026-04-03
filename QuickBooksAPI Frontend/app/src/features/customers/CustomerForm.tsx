import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/types';
import { Form } from '@/components/ui/form';
import { customerFormSchema, type CustomerFormValues } from './customerFormSchema';
import { CustomerFormFields } from './CustomerFormFields';

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
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6">
        <CustomerFormFields
          onCancel={onCancel}
          isSubmitting={isSubmitting}
          isEdit={!!customer}
        />
      </form>
    </Form>
  );
}
