import { createSlice } from '@reduxjs/toolkit';
import type { QBOBillHeader } from '@/types';

export interface BillUiState {
  isEditDialogOpen: boolean;
  isDeleteDialogOpen: boolean;
  selectedBill: QBOBillHeader | null;
  isSubmitting: boolean;
}

const initialState: BillUiState = {
  isEditDialogOpen: false,
  isDeleteDialogOpen: false,
  selectedBill: null,
  isSubmitting: false,
};

const billUiSlice = createSlice({
  name: 'billUi',
  initialState,
  reducers: {
    openEditDialog: (state, action: { payload: QBOBillHeader }) => {
      state.selectedBill = action.payload;
      state.isEditDialogOpen = true;
    },
    closeEditDialog: (state) => {
      state.isEditDialogOpen = false;
      state.selectedBill = null;
    },
    openDeleteDialog: (state, action: { payload: QBOBillHeader }) => {
      state.selectedBill = action.payload;
      state.isDeleteDialogOpen = true;
    },
    closeDeleteDialog: (state) => {
      state.isDeleteDialogOpen = false;
      state.selectedBill = null;
    },
    setSubmitting: (state, action: { payload: boolean }) => {
      state.isSubmitting = action.payload;
    },
  },
});

export const { openEditDialog, closeEditDialog, openDeleteDialog, closeDeleteDialog, setSubmitting } = billUiSlice.actions;

export default billUiSlice.reducer;
