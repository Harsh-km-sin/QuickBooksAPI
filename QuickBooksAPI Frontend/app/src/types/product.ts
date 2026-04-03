export interface Products {
  id: number;
  qboId: string;
  name: string;
  description: string | null;
  active: boolean;
  fullyQualifiedName: string;
  taxable: boolean;
  unitPrice: number;
  type: string;
  qtyOnHand: number | null;
  incomeAccountRefValue: string | null;
  incomeAccountRefName: string | null;
  expenseAccountRefValue: string | null;
  expenseAccountRefName: string | null;
  assetAccountRefValue: string | null;
  assetAccountRefName: string | null;
  purchaseCost: number;
  trackQtyOnHand: boolean;
  invStartDate: string | null;
  domain: string;
  sparse: boolean;
  syncToken: string;
  createTime: string;
  lastUpdatedTime: string;
  userId: number;
  realmId: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  active?: boolean;
  trackQtyOnHand?: boolean;
  type: string;
  incomeAccountRef?: { name: string; value: string };
  expenseAccountRef?: { name: string; value: string };
  assetAccountRef?: { name: string; value: string };
  unitPrice?: number;
  purchaseCost?: number;
  qtyOnHand?: number;
  invStartDate?: string;
}

export interface UpdateProductRequest {
  id: string;
  syncToken: string;
  sparse?: boolean;
  name?: string;
  type?: string;
  unitPrice?: number;
  purchaseCost?: number;
  qtyOnHand?: number;
  invStartDate?: string;
  incomeAccountRef?: { name: string; value: string };
  expenseAccountRef?: { name: string; value: string };
  assetAccountRef?: { name: string; value: string };
}

export interface DeleteProductRequest {
  id: string;
  syncToken: string;
  sparse: boolean;
  active: boolean;
}
