import { useState } from 'react';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { DialogFooter } from '@/components/ui/dialog';
import { Loader2 } from 'lucide-react';

export interface CustomerFormProps {
  customer?: Customer;
  onSubmit: (data: CreateCustomerRequest | UpdateCustomerRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function CustomerForm({ customer, onSubmit, onCancel, isSubmitting }: CustomerFormProps) {
  const [formData, setFormData] = useState<CreateCustomerRequest>({
    givenName: customer?.givenName || '',
    familyName: customer?.familyName || '',
    middleName: customer?.middleName || '',
    title: customer?.title || '',
    displayName: customer?.displayName || '',
    companyName: customer?.companyName || '',
    primaryEmailAddr: customer?.primaryEmailAddr ? { address: customer.primaryEmailAddr } : { address: '' },
    primaryPhone: customer?.primaryPhone ? { freeFormNumber: customer.primaryPhone } : { freeFormNumber: '' },
    billAddr: {
      line1: customer?.billAddrLine1 || '',
      city: customer?.billAddrCity || '',
      countrySubDivisionCode: customer?.billAddrCountrySubDivisionCode || '',
      postalCode: customer?.billAddrPostalCode || '',
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.displayName?.trim()) return;
    if (customer) {
      onSubmit({
        id: customer.qboId,
        syncToken: customer.syncToken,
        ...formData,
      } as UpdateCustomerRequest);
    } else {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="givenName">First Name *</Label>
          <Input
            id="givenName"
            value={formData.givenName}
            onChange={(e) => setFormData({ ...formData, givenName: e.target.value })}
            required
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="familyName">Last Name *</Label>
          <Input
            id="familyName"
            value={formData.familyName}
            onChange={(e) => setFormData({ ...formData, familyName: e.target.value })}
            required
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="displayName">Display Name *</Label>
        <Input
          id="displayName"
          value={formData.displayName}
          onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="companyName">Company Name</Label>
        <Input
          id="companyName"
          value={formData.companyName}
          onChange={(e) => setFormData({ ...formData, companyName: e.target.value })}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          value={formData.primaryEmailAddr?.address}
          onChange={(e) => setFormData({ ...formData, primaryEmailAddr: { address: e.target.value } })}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="phone">Phone</Label>
        <Input
          id="phone"
          value={formData.primaryPhone?.freeFormNumber}
          onChange={(e) => setFormData({ ...formData, primaryPhone: { freeFormNumber: e.target.value } })}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="address">Address Line 1</Label>
        <Input
          id="address"
          value={formData.billAddr?.line1}
          onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, line1: e.target.value } })}
        />
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="space-y-2">
          <Label htmlFor="city">City</Label>
          <Input
            id="city"
            value={formData.billAddr?.city}
            onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, city: e.target.value } })}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="state">State</Label>
          <Input
            id="state"
            value={formData.billAddr?.countrySubDivisionCode}
            onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, countrySubDivisionCode: e.target.value } })}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="postalCode">Postal Code</Label>
          <Input
            id="postalCode"
            value={formData.billAddr?.postalCode}
            onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, postalCode: e.target.value } })}
          />
        </div>
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {customer ? 'Update' : 'Create'} Customer
        </Button>
      </DialogFooter>
    </form>
  );
}
