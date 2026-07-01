import { configureStore } from '@reduxjs/toolkit';
import customerUiReducer from './slices/customerUiSlice';
import productUiReducer from './slices/productUiSlice';
import vendorUiReducer from './slices/vendorUiSlice';
import billUiReducer from './slices/billUiSlice';
import invoiceUiReducer from './slices/invoiceUiSlice';
import glReviewReducer from './slices/glReviewSlice';

export const store = configureStore({
  reducer: {
    customerUi: customerUiReducer,
    productUi: productUiReducer,
    vendorUi: vendorUiReducer,
    billUi: billUiReducer,
    invoiceUi: invoiceUiReducer,
    glReview: glReviewReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
