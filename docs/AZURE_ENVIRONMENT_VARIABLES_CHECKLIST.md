# Azure Environment Variables Checklist

Use this checklist to configure Azure before running the app in production.

## 1) QuickBooksAPI (Web App / App Service)

Set these in **QuickBooksAPI -> Configuration -> Application settings**:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `QuickBooks__AuthUrl` (optional if using default in appsettings)
- `QuickBooks__TokenUrl`
- `QuickBooks__RevokeUrl` (optional if using default in appsettings)
- `QuickBooks__Scopes`
- `QuickBooks__RequestURL`
- `QuickBooks__ClientId`
- `QuickBooks__ClientSecret`
- `QuickBooks__RedirectUri`
- `QuickBooks__FrontendBaseUrl`
- `QuickBooks__Environment` (`sandbox` or `production`)
- `ServiceBus__ConnectionString`
- `ServiceBus__QueueName` (default `qbo-full-sync`)
- `Cors__AllowedOrigins__0` (repeat indexes for multiple origins)
- `AzureOpenAI__Endpoint` (optional, for CFO Assistant LLM)
- `AzureOpenAI__ApiKey` (optional, for CFO Assistant LLM)
- `AzureOpenAI__DeploymentName` (optional, for CFO Assistant LLM)

## 2) SyncWorker (Function App)

Set these in **SyncWorker -> Environment variables -> App settings**:

- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- `DefaultConnection`
- `ServiceBusConnection`
- `QuickBooks__RequestURL`
- `QuickBooks__ClientId`
- `QuickBooks__ClientSecret`
- `QuickBooks__TokenUrl`

After configuration:

1. Restart the Function App.
2. Verify functions appear:
   - `FullSyncWorker`
   - `KpiSnapshotFunction`
   - `CloseIssuesFunction`
   - `ConsolidationFunction`

## 3) Frontend (Static Web App/App Service)

Set these in the frontend hosting environment:

- `VITE_API_URL`
- `VITE_LOG_LEVEL`
- `VITE_APP_ENV`

`VITE_API_URL` must point to your deployed QuickBooksAPI base URL.

