# User Secrets (Development)

Secrets have been removed from `appsettings.json` and `launchSettings.json`. For local development, use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

## Setup

User Secrets must run against the **project** that contains the .csproj (the web API), not the solution root.

**Important:** Use the path that matches your current directory.

- **If you're in the solution root** (folder that contains `QuickBooksAPI.sln` and a subfolder `QuickBooksAPI`):
  - Use: `--project QuickBooksAPI\QuickBooksAPI.csproj` (only **two** segments: folder + .csproj).
- **If you're already inside the project folder** (the one that contains `QuickBooksAPI.csproj`):
  - Use: `--project QuickBooksAPI.csproj` or omit `--project` and run the command without it.

```bash
# From solution root (C:\...\QuickBooksAPI):
dotnet user-secrets init --project QuickBooksAPI\QuickBooksAPI.csproj

# Or: go into project folder, then run without --project
cd QuickBooksAPI
dotnet user-secrets init
```

Then set the following (replace placeholder values with your real values).

- **From solution root:** use `--project QuickBooksAPI\QuickBooksAPI.csproj` (two segments).
- **From project folder (QuickBooksAPI):** use `--project QuickBooksAPI.csproj` or omit `--project`.

```bash
# Example from solution root (one level above the .csproj):
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=QuickbooksDB;Trusted_Connection=True;TrustServerCertificate=True;" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "Jwt:Key" "YOUR_SUPER_SECRET_KEY_AT_LEAST_32_CHARS" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "Jwt:Issuer" "QuickBooksAPI" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "Jwt:Audience" "QuickBooksAPIUsers" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "QuickBooks:ClientId" "YOUR_QUICKBOOKS_CLIENT_ID" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "QuickBooks:ClientSecret" "YOUR_QUICKBOOKS_CLIENT_SECRET" --project QuickBooksAPI\QuickBooksAPI.csproj
dotnet user-secrets set "QuickBooks:RedirectUri" "https://localhost:7135/api/auth/callback" --project QuickBooksAPI\QuickBooksAPI.csproj
```

## Production

In production, use **Azure Key Vault** (or equivalent) and never store secrets in config files or environment variables in source control.
