# Feature: Company & realm

Vertical slice for **QuickBooks-linked companies**, **realm selection**, and **company-scoped sync** — same pattern as `features/auth` (dedicated API module + barrel + hooks; pages import from `@/features/company`).

**Scope**

- List QuickBooks companies linked to the user (`connected-companies`).
- Realm selection for API calls (`X-Realm-Id` via `setRealmId` / `getRealmId` in `api/core.ts`).
- Full sync trigger and sync status (`/api/company/sync/full`, `/api/company/sync/status`).
- Disconnect a company (`/api/company/disconnect`).

**Frontend**

- **`index.ts`** — barrel: `companyApi`, `setRealmId`, `getRealmId`, `useConnectedCompanies` (import from `@/features/company`).
- **`useConnectedCompanies`** — shared loader for the connected-companies list (sidebar + Connected Companies page).
- **`companyApi`** — HTTP calls in `@/api/companyApi` (re-exported through the barrel).

**Backend**

- **`CompanyController`** — routes under `api/company/*`; implementation file: `QuickBooksAPI/Features/Companies/CompanyController.cs` (vertical slice entry point, not under `Controllers/`).

**Related**

- [features/auth/README.md](../auth/README.md) — JWT session and QuickBooks OAuth before listing companies.

**Related pages**

- `pages/ConnectedCompanies.tsx`
- `components/MainLayout.tsx` (sidebar realm switcher)
