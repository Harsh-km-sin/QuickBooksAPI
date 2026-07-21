# Azure Setup Check: `SyncWorker` (FullSync + CFO Timers)

This document explains what needs to be configured in Azure for your `SyncWorker` Function App to run:

- **Full sync** (`FullSyncWorker`) from **Service Bus** (`qbo-full-sync`)
- **CFO analytics timers**:
  - `KpiSnapshotFunction`
  - `CloseIssuesFunction`
  - `ConsolidationFunction`

It’s written to help you quickly identify what is incomplete when the Function App exists but the functions don’t run.

---

## 0) Quick mental model

- **Service Bus** is just a message queue.
- **Function App** is the compute host that actually runs your code.
- **TimerTrigger** functions run on a schedule, but only inside a deployed Function App.

If your timer functions aren’t doing anything, the usual causes are:
- the functions were not deployed into the Function App, or
- required app settings are missing, or
- the functions are disabled / the app isn’t running.

---

## 1) `FullSyncWorker` (Service Bus trigger) — what must be in place

### 1.1 Queue + trigger wiring

In code, your worker uses:
- Queue name: `qbo-full-sync`
- Connection setting name: `ServiceBusConnection`

So in Azure you must ensure:
- Azure Service Bus namespace + **queue** `qbo-full-sync` exists
- Messages are actually being sent to `qbo-full-sync` by the API

**How to verify**
- Open the Service Bus queue in Azure Portal and check:
  - message count increases when you start a full sync from the API

### 1.2 Function App app settings (required)

Your worker needs the following configuration values in the Function App “Application settings”:

1. `AzureWebJobsStorage`
   - For timers and Functions runtime. Usually set automatically when you create the Function App.
2. `FUNCTIONS_WORKER_RUNTIME`
   - Expected: `dotnet-isolated`
3. `ServiceBusConnection`
   - Must point to the same Service Bus namespace/queue your API uses.
4. `DefaultConnection`
   - Must point to the SQL DB where you created the warehouse + CFO tables.
5. QuickBooks API credentials used by the sync services:
   - `QuickBooks__RequestURL`
   - `QuickBooks__ClientId`
   - `QuickBooks__ClientSecret`
   - `QuickBooks__TokenUrl`

These are present in your local `SyncWorker/local.settings.json`, but **must also exist in Azure** for deployed runs.

### 1.3 Verify `FullSyncWorker` is deployed and enabled

In Azure Portal → your **SyncWorker Function App**:
- Go to **Functions**
- Confirm you see a function named `FullSyncWorker`
- Ensure it is **Enabled**

**How to verify it’s running**
- Run a full sync from your API / UI
- Then check:
  - Function logs for `Full sync completed`
  - SQL tables updating (warehouse rebuild, etc.)

---

## 2) CFO timers inside the same Function App — what must be in place

Your CFO timers are defined via **TimerTrigger** attributes in code. They run on a schedule, but only if the function is deployed and the Function App runtime can access:
- SQL (`DefaultConnection`)
- Functions storage (`AzureWebJobsStorage`)

### 2.1 Required app settings for timers

At minimum, your Function App must have:
- `AzureWebJobsStorage` (required for all TimerTriggers)
- `DefaultConnection` (required because the timers write into your DB)
- `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`

`ServiceBusConnection` is not required for the timers, unless your timer logic indirectly uses it (your timer code currently reads warehouse data and writes KPI/close/consolidation tables).

### 2.2 Verify the 3 timer functions are deployed

In Azure Portal → **SyncWorker Function App** → **Functions**:

You should see:
- `KpiSnapshotFunction`
- `CloseIssuesFunction`
- `ConsolidationFunction`

If you **don’t** see these in the Azure “Functions” list:
- the code containing them wasn’t deployed to this Function App, or
- you deployed a different project/build/Function App.

### 2.3 Timers may be disabled / app may be stopped

Even if the functions exist, timers won’t run if:
- the Function App is stopped/paused
- the specific timer function is disabled

**How to verify**
- Open each function → confirm it’s **Enabled**
- Look for a **Next run time**

---

## 3) What “incomplete setup” typically looks like

### Case A: `FullSyncWorker` works, but timers do nothing
Most common causes:
- timers were not deployed into the Function App, OR
- Function App app settings are incomplete (`DefaultConnection` missing in Azure), OR
- `AzureWebJobsStorage` misconfigured

### Case B: no functions are shown at all
Most common causes:
- you created the Function App but did not deploy/publish the code into it

### Case C: functions show, but no DB updates happen
Most common causes:
- missing `DefaultConnection`
- DB permission issue
- function fails with an exception (check logs)

---

## 4) Recommended verification steps (fast)

1. In Azure → SyncWorker Function App → **Functions**
   - Confirm all 4 functions exist: `FullSyncWorker`, `KpiSnapshotFunction`, `CloseIssuesFunction`, `ConsolidationFunction`
2. Check Application settings
   - Confirm `DefaultConnection` and `AzureWebJobsStorage` exist
3. Manually run once (if the portal allows “Run”)
   - Run each timer function once to force immediate DB writes
4. Verify DB
   - `kpi_snapshot`
   - `close_issues`
   - `fact_consolidated_pnl`

---

## 5) If you want, I can pinpoint what’s missing

If you paste these from Azure (screenshots or text), I can tell you exactly what’s incomplete:
- SyncWorker Function App → **Functions** list
- SyncWorker Function App → **Application settings** (keys only; redact secrets)

