## Remaining TODOs for Harsh – CFO Analytics & Intelligence

### 1. Database tasks

- [ ] **Run analytics/CFO scripts on the main app database**
  - [ ] `Scripts/CreateAnomalyEvents.sql` – creates `anomaly_events`
  - [ ] `Scripts/CreateKpiSnapshot.sql` – creates `kpi_snapshot`
  - [ ] `Scripts/CreateForecastTables.sql` – creates `forecast_scenarios`, `forecast_results`
  - [ ] `Scripts/CreateCloseIssues.sql` – creates `close_issues`
  - [ ] `Scripts/CreateConsolidationTables.sql` – creates `dim_entity`, `fact_consolidated_pnl`
- [ ] **Seed and maintain `dim_entity`**
  - [ ] Insert one leaf `dim_entity` row per QuickBooks realm (company) you connect
  - [ ] Insert parent / consolidated entities with `ParentEntityId` set
  - [ ] Mark consolidated parents with `IsConsolidatedNode = 1`
- [ ] **Verify DB permissions & indexing**
  - [ ] Confirm the app’s SQL user can read/write all new tables
  - [ ] Add any extra indexes if needed based on production load

### 2. Azure / infrastructure tasks

- [ ] **Service Bus configuration**
  - [ ] Create/confirm Service Bus namespace
  - [ ] Create `qbo-full-sync` queue
  - [ ] In **QuickBooksAPI** app settings, set `ServiceBus:ConnectionString` and (optionally) `ServiceBus:QueueName`
  - [ ] In **SyncWorker** app settings, set `ServiceBusConnection` to the same connection string
- [ ] **SyncWorker Functions app configuration**
  - [ ] Set `DefaultConnection` to the same SQL DB as the API
  - [ ] Confirm `FullSyncWorker` is running against `qbo-full-sync`
  - [ ] Enable and verify timer schedules:
    - [ ] `KpiSnapshotFunction` (e.g. `0 0 2 * * *` – daily at 02:00 UTC)
    - [ ] `CloseIssuesFunction` (e.g. `0 0 3 * * *` – daily at 03:00 UTC)
    - [ ] `ConsolidationFunction` (e.g. `0 0 4 1 * *` – monthly on day 1 at 04:00 UTC)
- [ ] **CFO Assistant LLM configuration (Azure OpenAI or equivalent)**
  - [ ] Provision Azure OpenAI resource and deployment
  - [ ] Add to **QuickBooksAPI** configuration:
    - `AzureOpenAI:Endpoint`
    - `AzureOpenAI:ApiKey`
    - `AzureOpenAI:DeploymentName`
  - [ ] (Optional) Move secrets into Azure Key Vault and wire configuration
- [ ] **General production hardening**
  - [ ] Configure CORS for the production frontend origin(s)
  - [ ] Configure JWT signing key, issuer, audience for prod
  - [ ] Enable Application Insights (or equivalent) for API + SyncWorker
  - [ ] Set up alerts on failed Functions executions and API 5xx rates

### 3. Validation / go-live checks

- [ ] **End-to-end test with a real/sandbox QuickBooks company**
  - [ ] Run a full sync and confirm warehouse data is populated
  - [ ] Verify anomalies and KPIs populate after timers run
  - [ ] Create a forecast scenario and confirm UI/API outputs
  - [ ] Ask the CFO Assistant a few key questions and validate answers
  - [ ] Check Close & Data Quality issues are detected and resolvable
  - [ ] Validate consolidated P&L for at least one parent entity

