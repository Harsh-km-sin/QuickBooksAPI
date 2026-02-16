import { createSlice } from '@reduxjs/toolkit';
import type { Products } from '@/types';

export interface ProductUiState {
  isCreateDialogOpen: boolean;
  isEditDialogOpen: boolean;
  isDeleteDialogOpen: boolean;
  selectedProduct: Products | null;
  isSubmitting: boolean;
}

const initialState: ProductUiState = {
  isCreateDialogOpen: false,
  isEditDialogOpen: false,
  isDeleteDialogOpen: false,
  selectedProduct: null,
  isSubmitting: false,
};

const productUiSlice = createSlice({
  name: 'productUi',
  initialState,
  reducers: {
    openCreateDialog: (state) => {
      state.isCreateDialogOpen = true;
    },
    closeCreateDialog: (state) => {
      state.isCreateDialogOpen = false;
    },
    openEditDialog: (state, action: { payload: Products }) => {
      state.selectedProduct = action.payload;
      state.isEditDialogOpen = true;
    },
    closeEditDialog: (state) => {
      state.isEditDialogOpen = false;
      state.selectedProduct = null;
    },
    openDeleteDialog: (state, action: { payload: Products }) => {
      state.selectedProduct = action.payload;
      state.isDeleteDialogOpen = true;
    },
    closeDeleteDialog: (state) => {
      state.isDeleteDialogOpen = false;
      state.selectedProduct = null;
    },
    setSubmitting: (state, action: { payload: boolean }) => {
      state.isSubmitting = action.payload;
    },
  },
});

export const {
  openCreateDialog,
  closeCreateDialog,
  openEditDialog,
  closeEditDialog,
  openDeleteDialog,
  closeDeleteDialog,
  setSubmitting,
} = productUiSlice.actions;

export default productUiSlice.reducer;
