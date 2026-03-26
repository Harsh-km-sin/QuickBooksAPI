# End-to-End Deployment Guide (UI + Backend + SyncWorker + DB)

## What this guide covers

- Deploy **QuickBooksAPI** (ASP.NET Web API)
- Deploy **SyncWorker** (Azure Functions, Service Bus trigger + timer jobs)
- Deploy the **Frontend UI** (Vite React)
- Ensure all CFO features (Warehouse analytics, Anomalies, KPIs, Forecast, CFO Assistant, Close/Data Quality, Consolidation) work end-to-end

## Prerequisites

- You already created the database schema and ran the CFO scripts (if not, see step 1).
- You have an Azure SQL connection string and valid app secrets for QuickBooks OAuth + JWT.

---

## 1) Database (already deployed, but here are the required scripts)

Even though you said DB is already created, make sure the following objects exist:

### Phase 2
- `Scripts/CreateAnomalyEvents.sql` -> `anomaly_events`
- `Scripts/CreateKpiSnapshot.sql` -> `kpi_snapshot`

### Phase 3
- `Scripts/CreateForecastTables.sql` -> `forecast_scenarios`, `forecast_results`
- `Scripts/CreateCloseIssues.sql` -> `close_issues`
- `Scripts/CreateConsolidationTables.sql` -> `dim_entity`, `fact_consolidated_pnl`

If any of these tables are missing, the worker/API functions will fail when they try to read/write them.

---

## 2) Azure resources you need

### Required
- **Azure SQL Database** (or SQL Server VM) hosting `QuickbooksDB`
- **Service Bus namespace**
  - Queue name: `qbo-full-sync` (your code listens to `qbo-full-sync`)
- **Azure Function App storage**
  - Timers require Function host storage via `AzureWebJobsStorage`

### Deploy targets
- **QuickBooksAPI** (Web App / App Service)
- **SyncWorker** (Function App; in your case it is named `SyncWorker` / your deployed app is effectively “Qbo_sync bus”)

### Optional
- **Application Insights** (recommended for troubleshooting)
- **Azure OpenAI** (only if you want LLM summarization enabled for the CFO Assistant)

---

## 3) Deploy Backend: QuickBooksAPI (UI -> API -> enqueue SyncWorker)

### 3.1 Publish QuickBooksAPI

- Deploy the **QuickBooksAPI** project from Visual Studio to your Azure Web App.

### 3.2 Configure Application settings (App Service)

Set these in **QuickBooksAPI → Configuration → Application settings**:

- `DefaultConnection` (SQL connection string to the same DB)
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `ServiceBus:ConnectionString` (Service Bus connection string)
- `ServiceBus:QueueName` (optional; defaults to `qbo-full-sync` in code)

QuickBooks OAuth configuration (names must match your code/environment variables):
- `QuickBooks__RequestURL`
- `QuickBooks__ClientId`
- `QuickBooks__ClientSecret`
- `QuickBooks__TokenUrl`

Frontend/UX compatibility (recommended):
- `Cors:AllowedOrigins` (so browser requests are accepted in production)

### 3.3 Verify

- Open `QuickBooksAPI` Swagger and confirm it loads.
- Perform login and a “connect company” flow to verify QuickBooks OAuth redirects work with your Azure URL (not localhost).

---

## 4) Deploy Backend: SyncWorker (Service Bus full sync + CFO timers)

Your SyncWorker contains:
- `FullSyncWorker` (Service Bus trigger from queue `qbo-full-sync`)
- CFO timer jobs:
  - `KpiSnapshotFunction` (daily)
  - `CloseIssuesFunction` (daily)
  - `ConsolidationFunction` (monthly)

### 4.1 Publish SyncWorker

- Deploy the **SyncWorker** project to the **Function App** you created (your app called `SyncWorker` / “Qbo_sync bus”).

### 4.2 Configure Function App application settings (Function App)

In **SyncWorker → Configuration → Application settings / Environment variables**, ensure the following keys exist (names matter):

Required for startup + timers:
- `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- `DefaultConnection` = SQL connection string to the DB
- `AzureWebJobsStorage` = your Function storage connection (Function host storage)

Required for `FullSyncWorker`:
- `ServiceBusConnection` = Service Bus connection string

QuickBooks configuration (needed by sync services):
- `QuickBooks__RequestURL`
- `QuickBooks__ClientId`
- `QuickBooks__ClientSecret`
- `QuickBooks__TokenUrl`

### 4.3 Verify functions are deployed

In the Function App portal, check:

- **Functions** tab shows:
  - `FullSyncWorker`
  - `KpiSnapshotFunction`
  - `CloseIssuesFunction`
  - `ConsolidationFunction`

If the Functions list is empty, the host usually failed to start (most commonly because `DefaultConnection` or required storage settings are missing).

### 4.4 Timers: do you need to create anything in Azure?

- No. Timer schedules are defined in code using `[TimerTrigger("...")]`.
- You only need the Function App deployed + storage configured + functions enabled.

---

## 5) Deploy Frontend UI (Vite React)

### 5.1 Create environment config

In `QuickBooksAPI Frontend/app/.env` set:
- `VITE_API_URL=https://<your-quickbooksapi-base-url>`

Example (prod):
- `VITE_API_URL=https://your-quickbooksapi.azurewebsites.net`

### 5.2 Build & deploy

- Deploy the built frontend (Vite output) to your hosting solution (App Service Static Web Apps, CDN, etc.).

### 5.3 Verify CORS

- Ensure `QuickBooksAPI` has `Cors:AllowedOrigins` including the frontend origin.

---

## 6) End-to-end verification checklist (quick)

1. Load UI
- Login works.
- “Connected Companies” loads.

2. Connect a QuickBooks company
- OAuth callback works using Azure RedirectUri (not localhost).

3. Start Full Sync
- Trigger full sync from UI.
- Confirm Service Bus queue `qbo-full-sync` gets messages.
- Confirm `FullSyncWorker` runs and warehouse rebuild completes.

4. Verify analytics outputs
- After full sync:
  - `anomaly_events` should be populated (Phase 2 anomalies run after warehouse rebuild).
- After timers:
  - `kpi_snapshot` gets daily rows (`KpiSnapshotFunction`)
  - `close_issues` gets daily rows (`CloseIssuesFunction`)
  - `fact_consolidated_pnl` gets monthly rows (`ConsolidationFunction`)

5. Verify CFO features in UI
- `Forecast` page can create and load forecast scenarios.
- `CFO Assistant` page returns answers (with citations; LLM may be optional depending on Azure OpenAI config).
- `CloseAssistant` page lists close/data-quality issues.
- Dashboard consolidation toggle displays consolidated P&L (after consolidation runs).

---

## 7) Troubleshooting (most common)

### “No functions” in SyncWorker Function App portal
- Check that `DefaultConnection` exists in Function App application settings.
- Check `AzureWebJobsStorage` exists.
- Check `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`.
- Check logs / Log stream for startup errors.

### Timers not running
- Timers require Azure Function host storage (`AzureWebJobsStorage`).
- Verify functions are enabled and have a “Next run time”.

### OAuth callback fails in Azure
- Update Intuit RedirectUri to your Azure API URL:
  - `/api/auth/callback`

