# Feature: Auth (frontend)

Vertical slice for **authentication**, **session/realm state**, **QuickBooks OAuth connect**, and **login/register** screens.

- **Imports:** `@/features/auth` (barrel): `AuthProvider`, `useAuth`, `useQuickBooks`, `Login`, `Register`.
- **HTTP:** `authApi` from `@/api/authApi` for login, sign-up, logout, OAuth URL, and callback URL construction.
- **Storage helpers:** `getToken`, `setToken`, `clearAuth`, `parseJwt`, `getRealmId`, `setRealmId` from `@/api/core`.
- **Routing:** `App.tsx` imports `Login` / `Register` from this package; `pages/index.ts` re-exports them for any barrel imports from `@/pages`.

**Related:** [features/company/README.md](../company/README.md) (connected companies and `X-Realm-Id` after login).

Backend documentation: `QuickBooksAPI/Features/Auth/README.md`.
