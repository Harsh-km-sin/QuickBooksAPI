import { createSlice } from '@reduxjs/toolkit';
import type { Customer } from '@/types';

export interface CustomerUiState {
  isCreateDialogOpen: boolean;
  isEditDialogOpen: boolean;
  isDeleteDialogOpen: boolean;
  selectedCustomer: Customer | null;
  isSubmitting: boolean;
}

const initialState: CustomerUiState = {
  isCreateDialogOpen: false,
  isEditDialogOpen: false,
  isDeleteDialogOpen: false,
  selectedCustomer: null,
  isSubmitting: false,
};

const customerUiSlice = createSlice({
  name: 'customerUi',
  initialState,
  reducers: {
    openCreateDialog: (state) => {
      state.isCreateDialogOpen = true;
    },
    closeCreateDialog: (state) => {
      state.isCreateDialogOpen = false;
    },
    openEditDialog: (state, action: { payload: Customer }) => {
      state.selectedCustomer = action.payload;
      state.isEditDialogOpen = true;
    },
    closeEditDialog: (state) => {
      state.isEditDialogOpen = false;
      state.selectedCustomer = null;
    },
    openDeleteDialog: (state, action: { payload: Customer }) => {
      state.selectedCustomer = action.payload;
      state.isDeleteDialogOpen = true;
    },
    closeDeleteDialog: (state) => {
      state.isDeleteDialogOpen = false;
      state.selectedCustomer = null;
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
} = customerUiSlice.actions;

export default customerUiSlice.reducer;
