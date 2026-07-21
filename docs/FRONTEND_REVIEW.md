# QuickBooks API Frontend - Review

This document reviews the **QuickBooksAPI Frontend** (`QuickBooksAPI Frontend/app/`) against the FRONTEND_PROMPT requirements and the backend API.

---

## Summary

| Category | Status | Notes |
|----------|--------|-------|
| Tech Stack | ✅ | React, TypeScript, Vite, Tailwind, Radix UI, Recharts, Zustand available |
| Authentication | ✅ | Login, SignUp, JWT, protected routes |
| API Client | ✅ | Centralized, Authorization + X-Realm-Id, error handling |
| Entity Management | ✅ | All 7 entities with list/sync/CRUD where applicable |
| Types | ✅ | Full TypeScript interfaces matching backend |
| Dashboard | ✅ | Stats cards, charts (Recharts), QuickBooks connect |
| Realm Support | ✅ | Multi-company switcher, X-Realm-Id header |
| QuickBooks OAuth | ✅ | Connect flow, getOAuthUrl, redirect |
| UI/UX | ✅ | Sidebar, tables, forms, modals, toasts, dark/light mode |

---

## What's Implemented Well

### 1. Folder Structure
- `src/api/` – API client
- `src/context/` – AuthContext
- `src/hooks/` – useCustomers, useProducts, useVendors, useBills, useInvoices, useChartOfAccounts, useJournalEntries, useDashboardStats, useQuickBooks
- `src/pages/` – Dashboard, Customers, Products, Vendors, Bills, Invoices, ChartOfAccounts, JournalEntries, Login, Register
- `src/components/ui/` – shadcn-style components
- `src/types/` – ApiResponse, entity interfaces, request DTOs

### 2. API Client (`api/client.ts`)
- `Authorization: Bearer <token>`
- `X-Realm-Id` from localStorage
- 401 → clearAuth, redirect to /login
- 403, 429, 500 handled with ApiError
- `X-Correlation-Id` captured
- Base URL from `VITE_API_URL` (default https://localhost:7135)

### 3. Authentication
- AuthContext with login, signUp, logout
- JWT in sessionStorage; realmId in localStorage
- parseJwt for UserId, Name, RealmIds
- Realm selection and persistence
- ProtectedRoute wrapper

### 4. Entity Types
- Customer, Products, Vendor, QBOBillHeader, QBOInvoiceHeader, ChartOfAccounts, QBOJournalEntryHeader
- Create/Update/Delete request DTOs aligned with backend

### 5. Dashboard
- Stats from list endpoints (customers, products, vendors, bills, invoices)
- Bar chart (entity overview)
- Pie chart (financial overview)
- QuickBooks connect button
- Connected status badge

### 6. Customers Page (Example)
- List, search, sync, create, edit, delete
- CustomerForm with validation
- Confirmation dialogs
- Loading states, empty states

---

## Minor Issues / Improvements

### 1. TanStack Query Not Used
- FRONTEND_PROMPT recommends TanStack Query for server state
- Current implementation uses useState + useEffect in hooks
- TanStack Query is in `package.json` but unused
- **Recommendation:** Optionally refactor hooks to use `useQuery` / `useMutation` for caching and invalidation

### 2. Customer Create Response Type
- Backend returns `ApiResponse<string>` (QuickBooks raw JSON)
- Frontend `customerApi.create` uses `apiClient.post<number>`
- Behavior is fine (only `success` is checked), but TypeScript type is inaccurate
- **Fix:** Use `apiClient.post<string>` for create/update/delete where backend returns string

### 3. OAuth Callback Handling
- Backend callback: `GET /api/auth/callback?code=...&state=...&realmId=...`
- QuickBooks redirects to backend URL (e.g. `https://localhost:7135/api/auth/callback?...`)
- Frontend may need a route or page to receive redirect if using frontend-based OAuth flow
- **Current:** Connect button redirects to Intuit; Intuit redirects to backend. Backend handles callback. User must return to app manually.
- **Enhancement:** Configure QuickBooks redirect to `https://localhost:5173/callback` (or similar) and add a frontend callback page that extracts `code`, `state`, `realmId` from URL and calls `authApi.handleCallback()`, then redirects to Dashboard. (Requires backend RedirectUri change.)

### 4. No React Router
- App uses `window.location.pathname` and manual routing
- FRONTEND_PROMPT suggests React Router v6
- **Recommendation:** Add React Router for cleaner routing, nested routes, and programmatic navigation

### 5. CreateCustomerRequest – Display Name
- Backend requires `DisplayName` for create
- CustomerForm uses `displayName` but does not enforce `required` for create
- **Fix:** Mark displayName as required in create flow

---

## Backend CORS Configuration

Ensure the backend `appsettings.Development.json` or `appsettings.json` includes the frontend origin:

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

(Vite default dev port is 5173.)

---

## Verification Checklist

| Item | Status |
|------|--------|
| All entity list endpoints called | ✅ |
| All entity sync endpoints called | ✅ |
| Customer CRUD | ✅ |
| Product CRUD | ✅ |
| Vendor CRUD + softDelete | ✅ |
| Bill CRUD | ✅ |
| Invoice CRUD + void | ✅ |
| Chart of Accounts list/sync | ✅ |
| Journal Entry list/sync | ✅ |
| Login/SignUp | ✅ |
| QuickBooks connect (OAuth URL) | ✅ |
| X-Realm-Id sent on requests | ✅ |
| JWT in Authorization header | ✅ |
| 401 redirects to login | ✅ |
| Toasts for success/error | ✅ |
| Dark/light theme | ✅ |
| Dashboard charts | ✅ |
| Realm switcher | ✅ (in AuthContext) |

---

## Conclusion

The frontend is well aligned with the FRONTEND_PROMPT and backend API. It covers auth, realm handling, all entities, dashboard, and UI components. The main optional improvements are:

1. Use TanStack Query for server state (currently present but unused)
2. Add React Router
3. Adjust Create/Update/Delete response types from `number` to `string` where the backend returns string
4. Optional: frontend OAuth callback page if redirect URI points to the frontend
