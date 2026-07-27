import { createSlice } from '@reduxjs/toolkit';
import type { QBOInvoiceHeader } from '@/types';

export interface InvoiceUiState {
  isCreateDialogOpen: boolean;
  isEditDialogOpen: boolean;
  isDeleteDialogOpen: boolean;
  isVoidDialogOpen: boolean;
  selectedInvoice: QBOInvoiceHeader | null;
  isSubmitting: boolean;
}

const initialState: InvoiceUiState = {
  isCreateDialogOpen: false,
  isEditDialogOpen: false,
  isDeleteDialogOpen: false,
  isVoidDialogOpen: false,
  selectedInvoice: null,
  isSubmitting: false,
};

const invoiceUiSlice = createSlice({
  name: 'invoiceUi',
  initialState,
  reducers: {
    openCreateDialog: (state) => {
      state.isCreateDialogOpen = true;
    },
    closeCreateDialog: (state) => {
      state.isCreateDialogOpen = false;
    },
    openEditDialog: (state, action: { payload: QBOInvoiceHeader }) => {
      state.selectedInvoice = action.payload;
      state.isEditDialogOpen = true;
    },
    closeEditDialog: (state) => {
      state.isEditDialogOpen = false;
      state.selectedInvoice = null;
    },
    openDeleteDialog: (state, action: { payload: QBOInvoiceHeader }) => {
      state.selectedInvoice = action.payload;
      state.isDeleteDialogOpen = true;
    },
    closeDeleteDialog: (state) => {
      state.isDeleteDialogOpen = false;
      state.selectedInvoice = null;
    },
    openVoidDialog: (state, action: { payload: QBOInvoiceHeader }) => {
      state.selectedInvoice = action.payload;
      state.isVoidDialogOpen = true;
    },
    closeVoidDialog: (state) => {
      state.isVoidDialogOpen = false;
      state.selectedInvoice = null;
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
  openVoidDialog,
  closeVoidDialog,
  setSubmitting,
} = invoiceUiSlice.actions;

export default invoiceUiSlice.reducer;

