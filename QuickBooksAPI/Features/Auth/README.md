# Feature: Auth (API)

Vertical slice entry point for **authentication and QuickBooks OAuth**: sign-up, login, logout, OAuth URL generation, and OAuth callback redirect.

- **Controller:** `AuthController` — routes `api/auth/*` (same as before relocation).
- **Dependencies:** `IAuthService`, `IOptions<QuickBooksOptions>` (for callback redirect base URL).

Shared DTOs: `QuickBooksAPI.API.DTOs.Request` / `Response`.

**Related:** [Companies](../Companies/README.md) (realm selection and sync after OAuth).

Frontend documentation: `QuickBooksAPI Frontend/app/src/features/auth/README.md`.
