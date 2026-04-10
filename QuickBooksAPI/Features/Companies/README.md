# Feature: Companies (API)

Vertical slice entry point for **QuickBooks-linked companies**: full sync, sync status, disconnect, and listing connected companies.

- **Controller:** `CompanyController` — routes `api/company/*` (unchanged from a consumer perspective; **moved** from `Controllers/` into this folder for feature-sliced layout).
- **Dependencies:** `IRequestContext`, `ISyncService`, `IAuthService`.

**Related:** [Auth](../Auth/README.md) (JWT and QuickBooks OAuth before company-scoped calls).

**Frontend:** The React app uses `api/company/*` via `companyApi` and the `features/company` barrel (`useConnectedCompanies`, realm helpers). See `QuickBooksAPI Frontend/app/src/features/company/README.md`.
