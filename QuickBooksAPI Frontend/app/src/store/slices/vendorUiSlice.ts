import { createSlice } from '@reduxjs/toolkit';
import type { Vendor } from '@/types';

export interface VendorUiState {
  isCreateDialogOpen: boolean;
  isEditDialogOpen: boolean;
  isDeleteDialogOpen: boolean;
  selectedVendor: Vendor | null;
  isSubmitting: boolean;
}

const initialState: VendorUiState = {
  isCreateDialogOpen: false,
  isEditDialogOpen: false,
  isDeleteDialogOpen: false,
  selectedVendor: null,
  isSubmitting: false,
};

const vendorUiSlice = createSlice({
  name: 'vendorUi',
  initialState,
  reducers: {
    openCreateDialog: (state) => {
      state.isCreateDialogOpen = true;
    },
    closeCreateDialog: (state) => {
      state.isCreateDialogOpen = false;
    },
    openEditDialog: (state, action: { payload: Vendor }) => {
      state.selectedVendor = action.payload;
      state.isEditDialogOpen = true;
    },
    closeEditDialog: (state) => {
      state.isEditDialogOpen = false;
      state.selectedVendor = null;
    },
    openDeleteDialog: (state, action: { payload: Vendor }) => {
      state.selectedVendor = action.payload;
      state.isDeleteDialogOpen = true;
    },
    closeDeleteDialog: (state) => {
      state.isDeleteDialogOpen = false;
      state.selectedVendor = null;
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
} = vendorUiSlice.actions;

export default vendorUiSlice.reducer;
