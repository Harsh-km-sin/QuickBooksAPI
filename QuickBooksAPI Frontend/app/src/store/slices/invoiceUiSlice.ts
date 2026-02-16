import { createSlice } from '@reduxjs/toolkit';
import type { QBOInvoiceHeader } from '@/types';

export interface InvoiceUiState {
  isDeleteDialogOpen: boolean;
  isVoidDialogOpen: boolean;
  selectedInvoice: QBOInvoiceHeader | null;
  isSubmitting: boolean;
}

const initialState: InvoiceUiState = {
  isDeleteDialogOpen: false,
  isVoidDialogOpen: false,
  selectedInvoice: null,
  isSubmitting: false,
};

const invoiceUiSlice = createSlice({
  name: 'invoiceUi',
  initialState,
  reducers: {
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
  openDeleteDialog,
  closeDeleteDialog,
  openVoidDialog,
  closeVoidDialog,
  setSubmitting,
} = invoiceUiSlice.actions;

export default invoiceUiSlice.reducer;
