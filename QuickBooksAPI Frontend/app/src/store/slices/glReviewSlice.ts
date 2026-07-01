import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

const STORAGE_KEY = 'gl_selected_run';

function readPersisted(): number | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const n = Number(raw);
    return Number.isFinite(n) ? n : null;
  } catch {
    return null;
  }
}

export interface GlReviewState {
  /** User-chosen run. `null` means "fall back to the latest completed run". */
  selectedRunId: number | null;
}

const initialState: GlReviewState = {
  selectedRunId: readPersisted(),
};

const glReviewSlice = createSlice({
  name: 'glReview',
  initialState,
  reducers: {
    setSelectedRun: (state, action: PayloadAction<number | null>) => {
      state.selectedRunId = action.payload;
      try {
        if (action.payload == null) localStorage.removeItem(STORAGE_KEY);
        else localStorage.setItem(STORAGE_KEY, String(action.payload));
      } catch {
        /* ignore storage failures (private mode, etc.) */
      }
    },
  },
});

export const { setSelectedRun } = glReviewSlice.actions;
export default glReviewSlice.reducer;
