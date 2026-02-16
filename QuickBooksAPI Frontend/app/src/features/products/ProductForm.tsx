import { useState } from 'react';
import type { Products, CreateProductRequest, UpdateProductRequest } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { DialogFooter } from '@/components/ui/dialog';
import { Loader2 } from 'lucide-react';

export interface ProductFormProps {
  product?: Products;
  onSubmit: (data: CreateProductRequest | UpdateProductRequest) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function ProductForm({ product, onSubmit, onCancel, isSubmitting }: ProductFormProps) {
  const [formData, setFormData] = useState<CreateProductRequest>({
    name: product?.name || '',
    description: product?.description || '',
    active: product?.active ?? true,
    type: product?.type || 'Service',
    unitPrice: product?.unitPrice || 0,
    purchaseCost: product?.purchaseCost || 0,
    qtyOnHand: product?.qtyOnHand || 0,
    trackQtyOnHand: product?.trackQtyOnHand || false,
    incomeAccountRef: product?.incomeAccountRefValue ? { value: product.incomeAccountRefValue, name: product.incomeAccountRefName || '' } : undefined,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (product) {
      onSubmit({
        id: product.qboId,
        syncToken: product.syncToken,
        ...formData,
      } as UpdateProductRequest);
    } else {
      onSubmit(formData);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Name *</Label>
        <Input
          id="name"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Input
          id="description"
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="type">Type</Label>
        <select
          id="type"
          value={formData.type}
          onChange={(e) => setFormData({ ...formData, type: e.target.value })}
          className="w-full h-10 px-3 rounded-md border border-input bg-background"
        >
          <option value="Service">Service</option>
          <option value="Inventory">Inventory</option>
          <option value="NonInventory">Non-Inventory</option>
        </select>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="unitPrice">Unit Price</Label>
          <Input
            id="unitPrice"
            type="number"
            step="0.01"
            value={formData.unitPrice}
            onChange={(e) => setFormData({ ...formData, unitPrice: parseFloat(e.target.value) })}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="purchaseCost">Purchase Cost</Label>
          <Input
            id="purchaseCost"
            type="number"
            step="0.01"
            value={formData.purchaseCost}
            onChange={(e) => setFormData({ ...formData, purchaseCost: parseFloat(e.target.value) })}
          />
        </div>
      </div>

      {formData.type === 'Inventory' && (
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="qtyOnHand">Quantity on Hand</Label>
            <Input
              id="qtyOnHand"
              type="number"
              value={formData.qtyOnHand}
              onChange={(e) => setFormData({ ...formData, qtyOnHand: parseInt(e.target.value) })}
            />
          </div>
        </div>
      )}

      <div className="flex items-center space-x-2">
        <Switch
          id="active"
          checked={formData.active}
          onCheckedChange={(checked) => setFormData({ ...formData, active: checked })}
        />
        <Label htmlFor="active">Active</Label>
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {product ? 'Update' : 'Create'} Product
        </Button>
      </DialogFooter>
    </form>
  );
}
