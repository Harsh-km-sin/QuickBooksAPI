import { useState } from 'react';
import type { Vendor, CreateVendorRequest, UpdateVendorRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { DialogFooter } from '@/components/ui/dialog';
import { Loader2 } from 'lucide-react';

export interface VendorFormProps {
  vendor?: Vendor;
  onSubmit: (data: CreateVendorRequest | UpdateVendorRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function VendorForm({ vendor, onSubmit, onCancel, isSubmitting }: VendorFormProps) {
  const [formData, setFormData] = useState<CreateVendorRequest>({
    displayName: vendor?.displayName || '',
    givenName: vendor?.givenName || '',
    familyName: vendor?.familyName || '',
    companyName: vendor?.companyName || '',
    primaryEmailAddr: vendor?.primaryEmailAddr ? { address: vendor.primaryEmailAddr } : { address: '' },
    primaryPhone: vendor?.primaryPhone ? { freeFormNumber: vendor.primaryPhone } : { freeFormNumber: '' },
    billAddr: {
      line1: vendor?.billAddrLine1 || '',
      city: vendor?.billAddrCity || '',
      countrySubDivisionCode: vendor?.billAddrCountrySubDivisionCode || '',
      postalCode: vendor?.billAddrPostalCode || '',
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (vendor) {
      onSubmit({ id: vendor.qboId, syncToken: vendor.syncToken, ...formData } as UpdateVendorRequest);
    } else {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="displayName">Display Name *</Label>
        <Input id="displayName" value={formData.displayName} onChange={(e) => setFormData({ ...formData, displayName: e.target.value })} required />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2"><Label htmlFor="givenName">First Name</Label><Input id="givenName" value={formData.givenName} onChange={(e) => setFormData({ ...formData, givenName: e.target.value })} /></div>
        <div className="space-y-2"><Label htmlFor="familyName">Last Name</Label><Input id="familyName" value={formData.familyName} onChange={(e) => setFormData({ ...formData, familyName: e.target.value })} /></div>
      </div>
      <div className="space-y-2"><Label htmlFor="companyName">Company Name</Label><Input id="companyName" value={formData.companyName} onChange={(e) => setFormData({ ...formData, companyName: e.target.value })} /></div>
      <div className="space-y-2"><Label htmlFor="email">Email</Label><Input id="email" type="email" value={formData.primaryEmailAddr?.address} onChange={(e) => setFormData({ ...formData, primaryEmailAddr: { address: e.target.value } })} /></div>
      <div className="space-y-2"><Label htmlFor="phone">Phone</Label><Input id="phone" value={formData.primaryPhone?.freeFormNumber} onChange={(e) => setFormData({ ...formData, primaryPhone: { freeFormNumber: e.target.value } })} /></div>
      <div className="space-y-2"><Label htmlFor="address">Address Line 1</Label><Input id="address" value={formData.billAddr?.line1} onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, line1: e.target.value } })} /></div>
      <div className="grid grid-cols-3 gap-4">
        <div className="space-y-2"><Label htmlFor="city">City</Label><Input id="city" value={formData.billAddr?.city} onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, city: e.target.value } })} /></div>
        <div className="space-y-2"><Label htmlFor="state">State</Label><Input id="state" value={formData.billAddr?.countrySubDivisionCode} onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, countrySubDivisionCode: e.target.value } })} /></div>
        <div className="space-y-2"><Label htmlFor="postalCode">Postal Code</Label><Input id="postalCode" value={formData.billAddr?.postalCode} onChange={(e) => setFormData({ ...formData, billAddr: { ...formData.billAddr!, postalCode: e.target.value } })} /></div>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
        <Button type="submit" disabled={isSubmitting}>{isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}{vendor ? 'Update' : 'Create'} Vendor</Button>
      </DialogFooter>
    </form>
  );
}
