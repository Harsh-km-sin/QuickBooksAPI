import { createSlice } from '@reduxjs/toolkit';
import type { QBOBillHeader } from '@/types';

export interface BillUiState {
  isDeleteDialogOpen: boolean;
  selectedBill: QBOBillHeader | null;
  isSubmitting: boolean;
}

const initialState: BillUiState = {
  isDeleteDialogOpen: false,
  selectedBill: null,
  isSubmitting: false,
};

const billUiSlice = createSlice({
  name: 'billUi',
  initialState,
  reducers: {
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

export const { openDeleteDialog, closeDeleteDialog, setSubmitting } = billUiSlice.actions;

export default billUiSlice.reducer;
