# Reports Sync — QBO Reports API Research (P&L + Balance Sheet)

Status: **research only, no implementation yet**. This informs the DB schema (tables/views/SPs) and sync-service design for `features/reports-sync`.

## 1. Scope

QuickBooks Online exposes a separate **Reports API**, distinct from the entity CRUD/Query API this codebase already integrates with (Invoices, Bills, Customers, etc.). It returns a pre-aggregated, hierarchical financial report document (not a list of rows we page through), for report types including `ProfitAndLoss` and `BalanceSheet` (also `CashFlow`, `TrialBalance`, `GeneralLedger`, `AgedReceivables`/`AgedPayables`, and per-dimension variants like `ProfitAndLossDetail`, `SalesByClassSummary`, etc. — out of scope for now).

**There is zero existing code for this in the repo** (confirmed by exhaustive grep across `.cs`, `.py`, `.sql`, `.md` — see §7). This is a greenfield feature.

## 2. Endpoint

```
GET {RequestURL}/{realmId}/reports/{ReportType}?{params}
```

- `ReportType` = `ProfitAndLoss` or `BalanceSheet` (case-sensitive path segment).
- Base URL reuses the **existing** `QuickBooks:RequestURL` config key already used by every other entity service (`QuickBooksService/Services/QuickBooks*Service.cs`):
  - Sandbox: `https://sandbox-quickbooks.api.intuit.com/v3/company`
  - Production: `https://quickbooks.api.intuit.com/v3/company`
- Auth: identical pattern to every existing entity call — `Authorization: Bearer {accessToken}` + `Accept: application/json`, token resolved/refreshed via the existing `IAuthService.RefreshTokenIfExpiredAsync` flow. No new OAuth scope needed; reports live under the same `com.intuit.quickbooks.accounting` scope already requested.
- **Gap to fill**: no `minorversion` query param is set anywhere in the codebase today. Newer report options/columns can require a minimum minor version. Recommend adding a `QuickBooks:MinorVersion` config key (none exists in `QuickBooksOptions` currently) and always sending `?minorversion=N&format=json`.

## 3. Request parameters

### Common to both reports

| Param | Notes |
|---|---|
| `start_date`, `end_date` | `YYYY-MM-DD`. **Both must be supplied together** — several independent reports confirm QBO silently ignores a lone `end_date` and falls back to current-year-to-date with no error. For a Balance Sheet "as of" snapshot, set `start_date == end_date`. |
| `date_macro` | Alternative to explicit dates. Standard values: `Today`, `Yesterday`, `This Week`, `Last Week`, `This Week-to-date`, `This Month`, `Last Month`, `This Month-to-date`, `This Fiscal Quarter`, `Last Fiscal Quarter`, `This Fiscal Quarter-to-date`, `This Fiscal Year`, `Last Fiscal Year`, `This Fiscal Year-to-date`, `Last Fiscal Year-to-date`. Mutually exclusive with explicit `start_date`/`end_date`. |
| `accounting_method` | `Cash` or `Accrual`. Should mirror the company's configured method — worth surfacing as a stored per-realm setting rather than hardcoding. |
| `summarize_column_by` | Splits the report into comparative columns. Values: `Total` (single column, default), `Month`, `Week`, `Days`, `Quarter`, `Year`, `Customers`, `Vendors`, `Classes`, `Departments`, `Employees`, `ProductsAndServices`. **Decided per discussion — see §9.** Not `Total`-only: sync at `Month` grain for a rolling 3-year window, let the UI compose month-aligned ranges (quarter/year/multi-month/trailing-N/YoY) from stored data. No live-fallback path for non-month-aligned custom ranges in v1. |
| `columns` | Comma-separated list of extra fields (only meaningful on **Detail**-style reports, e.g. `ProfitAndLossDetail`, which return one row per transaction and let you pick which transaction fields — `txn_date`, `doc_num`, `memo`, etc. — appear as columns). It is comma-separated simply because it's a multi-value QBO query param, same shape as `columns=txn_type,tx_date,doc_num` used elsewhere in the API. **Confirmed via real sample data (§4.1): it does not apply to `ProfitAndLoss`/`BalanceSheet` summary reports at all** — those always return exactly the account/dimension column plus one amount column per `summarize_column_by` bucket, decided by QBO itself, not by `columns`. Not needed now; only becomes relevant if/when we add a *Detail* report variant later. |
| `customer`, `vendor`, `class`, `department` | Comma-separated QBO IDs — dimensional filters. Not needed for a company-wide P&L/BS sync, but worth keeping in mind if the product later wants per-class or per-department reports. |
| `qzurl` | `true`/`false` — when true, each cell gets a deep-link URL back into QBO's UI. Not needed for our storage use case, adds payload weight — leave off. |
| `showrows=all&showcols=all` | Undocumented-but-known flag pair to include zero-value/inactive rows and columns, so periods with no activity still return a full account skeleton rather than a sparse report. Worth using for consistent row sets sync-over-sync. |
| `minorversion` | See gap noted above. |

### Report-specific behavior

- **ProfitAndLoss**: period-based (`start_date`→`end_date` range), returns Income / COGS / Gross Profit / Expenses / Net Operating Income / Other Income / Other Expenses / Net Income groupings.
- **BalanceSheet**: point-in-time by nature (Assets/Liabilities/Equity as of a date), but the API still requires a `start_date`/`end_date` pair per the above quirk — use the same value for both, or the company's fiscal-year start as `start_date` and the as-of date as `end_date` if you want QBO to compute retained-earnings roll-forward correctly (Balance Sheet math is sensitive to the implied period for net income rollup — needs a live sandbox check before finalizing our default).

## 4. Response JSON schema

All reports share one envelope shape: **`Header` / `Columns` / `Rows`**. This is the part that matters most for DB modeling.

```jsonc
{
  "Header": {
    "Time": "2026-07-21T10:00:00-07:00",
    "ReportName": "ProfitAndLoss",
    "DateMacro": "This Fiscal Year-to-date",   // present if date_macro was used
    "ReportBasis": "Accrual",
    "StartPeriod": "2026-01-01",
    "EndPeriod": "2026-07-21",
    "SummarizeColumnsBy": "Total",
    "Currency": "USD",
    "Option": [
      { "Name": "NoReportData", "Value": "false" }
    ]
  },
  "Columns": {
    "Column": [
      { "ColTitle": "", "ColType": "Account", "MetaData": [{ "Name": "ColKey", "Value": "account" }] },
      { "ColTitle": "Total", "ColType": "Money", "MetaData": [{ "Name": "ColKey", "Value": "total" }] }
    ]
  },
  "Rows": {
    "Row": [
      {
        "type": "Section",
        "group": "Income",
        "Header": { "ColData": [{ "value": "Income" }] },
        "Rows": {
          "Row": [
            {
              "type": "Data",
              "ColData": [
                { "value": "Sales", "id": "79" },
                { "value": "45000.00" }
              ]
            }
          ]
        },
        "Summary": { "ColData": [{ "value": "Total Income" }, { "value": "45000.00" }] }
      },
      {
        "type": "Section",
        "group": "COGS",
        "Header": { "ColData": [{ "value": "Cost of Goods Sold" }] },
        "Rows": { "Row": [ /* ... */ ] },
        "Summary": { "ColData": [{ "value": "Total COGS" }, { "value": "12000.00" }] }
      },
      {
        "type": "Section",
        "group": "GrossProfit",
        "Summary": { "ColData": [{ "value": "Gross Profit" }, { "value": "33000.00" }] }
      },
      {
        "type": "Section",
        "group": "Expenses",
        "Header": { "ColData": [{ "value": "Expenses" }] },
        "Rows": { "Row": [ /* leaf Data rows, one per expense account */ ] },
        "Summary": { "ColData": [{ "value": "Total Expenses" }, { "value": "9000.00" }] }
      },
      {
        "type": "Section",
        "group": "NetOperatingIncome",
        "Summary": { "ColData": [{ "value": "Net Operating Income" }, { "value": "24000.00" }] }
      },
      {
        "type": "Section",
        "group": "NetIncome",
        "Summary": { "ColData": [{ "value": "Net Income" }, { "value": "24000.00" }] }
      }
    ]
  }
}
```

Key structural rules:

- A `Row` is **either**:
  - a **leaf row** (`"type": "Data"`) with a flat `ColData: [{value, id?}, ...]` array — one entry per column, `id` present only on the account/dimension column (it's the QBO account/customer/vendor ID, useful as a join key back to `dbo.QBOAccount` or similar), or
  - a **group/section row** (`"type": "Section"`, with a `group` discriminator like `Income`, `COGS`, `GrossProfit`, `Expenses`, `NetOperatingIncome`, `OtherIncome`, `OtherExpenses`, `NetIncome` for P&L, and `Asset`, `Liability`, `Equity`/similar for Balance Sheet) — which recursively nests more `Rows.Row[]` and always carries its own `Summary.ColData` subtotal row, and often a `Header.ColData` section title row.
- `Header.Option` array is a generic name/value bag; `NoReportData: "true"` is the documented way to detect an empty report (e.g., a period with zero transactions) instead of an error.
- `Columns.Column[].MetaData` carries a `ColKey` (a stable machine key like `account`, `total`, or a period label) — more reliable to key off than `ColTitle`, which is a display string.
- This is a **recursive tree**, not a flat table — the natural DB shape is either (a) a flattened "one row per leaf account + subtotal, tagged with its section/group and depth" table, or (b) store `RawJson` verbatim and materialize a queryable flattened view via a stored proc / OPENJSON, mirroring the `RawJson` pattern already used for Invoice/Bill headers in this codebase. Recommend **both**: raw JSON for audit/replay, plus a flattened `ReportLine` table for querying, matching how other entities already keep a raw payload alongside typed columns.

### 4.1 Real sample response (not fabricated)

The illustrative JSON above was schematic. Below is a **genuine QBO sandbox response** (Craig's Design and Landscaping — QBO's standard public demo company), a `ProfitAndLoss` pull filtered by a single customer, `summarize_column_by=Total`, trimmed only where noted:

```json
{
  "Header": {
    "Time": "2022-09-14T12:20:17-07:00",
    "ReportName": "ProfitAndLoss",
    "ReportBasis": "Accrual",
    "StartPeriod": "2022-06-01",
    "EndPeriod": "2022-09-30",
    "SummarizeColumnsBy": "Total",
    "Currency": "USD",
    "Customer": "1",
    "Option": [
      { "Name": "AccountingStandard", "Value": "GAAP" },
      { "Name": "NoReportData", "Value": "false" }
    ]
  },
  "Columns": {
    "Column": [
      { "ColTitle": "", "ColType": "Account", "MetaData": [{ "Name": "ColKey", "Value": "account" }] },
      { "ColTitle": "Total", "ColType": "Money", "MetaData": [{ "Name": "ColKey", "Value": "total" }] }
    ]
  },
  "Rows": {
    "Row": [
      {
        "Header": { "ColData": [{ "value": "Income" }, { "value": "" }] },
        "Rows": {
          "Row": [
            {
              "Header": { "ColData": [{ "value": "Landscaping Services", "id": "45" }, { "value": "220.00" }] },
              "Rows": {
                "Row": [
                  {
                    "Header": { "ColData": [{ "value": "Job Materials", "id": "46" }, { "value": "" }] },
                    "Rows": {
                      "Row": [
                        { "ColData": [{ "value": "Fountains and Garden Lighting", "id": "48" }, { "value": "275.00" }], "type": "Data" },
                        { "ColData": [{ "value": "Plants and Soil", "id": "49" }, { "value": "150.00" }], "type": "Data" }
                      ]
                    },
                    "Summary": { "ColData": [{ "value": "Total Job Materials" }, { "value": "425.00" }] },
                    "type": "Section"
                  },
                  {
                    "Header": { "ColData": [{ "value": "Labor", "id": "51" }, { "value": "" }] },
                    "Rows": { "Row": [{ "ColData": [{ "value": "Maintenance and Repair", "id": "53" }, { "value": "50.00" }], "type": "Data" }] },
                    "Summary": { "ColData": [{ "value": "Total Labor" }, { "value": "50.00" }] },
                    "type": "Section"
                  }
                ]
              },
              "Summary": { "ColData": [{ "value": "Total Landscaping Services" }, { "value": "695.00" }] },
              "type": "Section"
            },
            { "ColData": [{ "value": "Pest Control Services", "id": "54" }, { "value": "-65.00" }], "type": "Data" }
          ]
        },
        "Summary": { "ColData": [{ "value": "Total Income" }, { "value": "630.00" }] },
        "type": "Section",
        "group": "Income"
      },
      { "Summary": { "ColData": [{ "value": "Gross Profit" }, { "value": "630.00" }] }, "type": "Section", "group": "GrossProfit" },
      {
        "Header": { "ColData": [{ "value": "Expenses" }, { "value": "" }] },
        "Summary": { "ColData": [{ "value": "Total Expenses" }, { "value": "" }] },
        "type": "Section",
        "group": "Expenses"
      },
      { "Summary": { "ColData": [{ "value": "Net Operating Income" }, { "value": "630.00" }] }, "type": "Section", "group": "NetOperatingIncome" },
      { "Summary": { "ColData": [{ "value": "Net Income" }, { "value": "630.00" }] }, "type": "Section", "group": "NetIncome" }
    ]
  }
}
```

What this real payload confirms/corrects vs. the schematic version above:

1. **Nesting goes deeper than 2 levels in practice**: `Income` → `Landscaping Services` (a sub-account acting as its own group) → `Job Materials` / `Labor` (further sub-groups) → leaf `Data` rows. A flattened table needs an actual `Depth`/`ParentRowId`, not just a fixed "section vs. leaf" flag — real charts of accounts nest arbitrarily.
2. **`group` is a top-level-only discriminator.** Only the five outermost sections (`Income`, `GrossProfit`, `Expenses`, `NetOperatingIncome`, `NetIncome` here) carry `"group"`. Nested sub-sections (`Landscaping Services`, `Job Materials`, `Labor`) are still `"type": "Section"` but have **no** `group` — don't rely on `group` being present at every level.
3. **"Virtual" summary-only rows exist** — `GrossProfit`, `NetOperatingIncome`, `NetIncome` have *only* a `Summary`, no `Header`/`Rows` at all (they're computed, not real accounts). A flattening routine must treat missing `Header`/`Rows` as normal, not an error case.
4. **Filter values round-trip into `Header`** — `"Customer": "1"` shows up because the request was filtered by customer. Any dimensional filter we apply (customer/vendor/class/department) will echo back in `Header`, which is a convenient way to record exactly what filter produced a given stored snapshot.
5. `Header.Option` is confirmed as a flat name/value array (`AccountingStandard`, `NoReportData`), not a fixed schema — treat it as an extensible bag when storing.

*(Source: a real captured QBO sandbox response, not Intuit's own docs — see §7.)*

## 5. Operational constraints

- **Rate limits**: reports are explicitly called out by Intuit as "resource-intensive" endpoints — **200 requests/minute per company**, vs. 500/min for standard entity endpoints, and a cap of **10 concurrent requests per company**. Throttled calls return HTTP 429 with QBO error code `003001` (`ThrottleExceeded`).
  - **Gap**: this codebase currently has **no outbound retry/backoff for any QBO call** (confirmed — no Polly, no 429/`Retry-After` handling anywhere). The only retry is a generic 2-attempt/2-second-fixed-delay wrapper in `SyncWorker/FullSyncEntitySyncRunner.cs` around each sync step as a whole, not per-HTTP-call, and it doesn't distinguish 429s. Reports sync should not reuse this as-is — needs real `Retry-After`-aware backoff given the tighter budget.
- **Cell limit**: hard limit of **400,000 cells** per response.
- **Column limit**: requesting 25+ columns risks timeouts/incomplete payloads — irrelevant if we stick to `summarize_column_by=Total`.
- **Date range**: Intuit's own guidance is to keep each request to **≤ 6 months** to stay well under cell limits and avoid slow responses — relevant if we ever support historical backfill (e.g., syncing 3 years of monthly P&L would mean chunking into ≤6-month windows, not one giant call).
- Reports return **one document per call** — there is no `STARTPOSITION`/`MAXRESULTS` paging concept here (unlike the `query` endpoint the Invoice/Bill sync uses), so the existing incremental-cursor pattern (`QBO_Sync_State.LastUpdatedAfter` + `MetaData.LastUpdatedTime` filtering) doesn't apply. A report "sync" is really "pull the current snapshot for a given period", and the meaningful cursor is **which date range/period was last pulled**, not a row-level watermark.

## 6. How this fits the existing codebase conventions

(Per direct inspection of the current sync architecture — see `docs/` and the Invoice/Bill sync path.)

- **Config**: reuse `QuickBooks:RequestURL` / `ClientId` / `ClientSecret` / token endpoints as-is (`Contracts/QuickBooksShared/Options/QuickBooksOptions.cs`). Add `QuickBooks:MinorVersion`.
- **Service layer**: add `IQuickBooksReportsService` in `QuickBooksService/Services/` alongside `QuickBooksInvoiceService.cs` et al., following the same `IHttpClientFactory` + `Bearer` token + `Accept: application/json` pattern — no shared low-level client to build on, this codebase hand-rolls `HttpClient` calls per entity service.
- **Sync orchestration**: add a `ProfitAndLossSyncService` / `BalanceSheetSyncService` (or one combined `ReportsSyncService`) in `QuickBooksAPI/Services/`, mirroring `InvoiceQboSyncService.cs`'s shape (token refresh → call → persist), but **without** the paging loop and **without** the `MetaData.LastUpdatedTime` incremental filter (neither applies to reports).
- **Trigger**: likely both a manual `[HttpGet("sync")]` controller endpoint (matching `InvoiceController.SyncInvoices()`) and a `SyncWorker/Steps/FullSyncProfitAndLossStep.cs` / `FullSyncBalanceSheetStep.cs` implementing `IFullSyncEntitySyncStep`, registered in the same runner as the other entity steps — for design consistency with the rest of the product, even though the underlying pull mechanics differ.
- **Sync-state tracking**: `QBO_Sync_State` (keyed `UserId, RealmId, EntityType`) could get `EntityType = 'ProfitAndLoss'` / `'BalanceSheet'` rows, but `LastUpdatedAfter` isn't the right semantic here — more useful would be tracking the **last successfully synced period** (e.g. `LastPeriodStart`/`LastPeriodEnd`) so a scheduled job knows whether "this month" has already been pulled. This likely needs either a small schema change to that table or a dedicated `ReportSyncState` table — a decision for the schema design pass.

## 7. Verification performed

- Endpoint shape, base URL, and auth-header construction cross-verified against the `node-quickbooks` open-source SDK's actual request-building code (`module.report`, `module.request`) — confirms `/reports/{ReportType}` sub-resource pattern, `Bearer` auth, and `minorversion`/`format=json` as standard query params, independent of Intuit's (JS-rendered, non-scrapable) official docs pages.
- Parameter names/values and response shape cross-checked against Intuit's own Medium engineering posts ("QuickBooks Online Reports API: Best practices and troubleshooting", "Upcoming changes to Reports APIs"), third-party integration guides (Zuplo, Knit), and community Q&A threads describing real request/response behavior (including the undocumented `start_date`+`end_date`-must-both-be-present quirk).
- **Real sample response obtained and reviewed** (§4.1) — a genuine captured QBO sandbox `ProfitAndLoss` response (Craig's Design and Landscaping, QBO's public demo company), not a fabricated example.
- Relational storage pattern (§10) cross-checked against a real production data-integration vendor's (SyncHub) shipped QBO→SQL schema for report tables, including actual column names/types.
- Confirmed via full-repo grep (`.cs`, `.py`, `.sql`, `.md`, excluding `node_modules`) that no `ProfitAndLoss`/`BalanceSheet`/report-sync code exists anywhere in this repo today.
- **Checked whether we can hit our own sandbox company directly** (rather than relying on public examples): `appsettings.Development.json` correctly points `QuickBooks:RequestURL` at the sandbox host, and `docs/USER_SECRETS.md` documents the supported local dev-secrets workflow (`dotnet user-secrets set QuickBooks:ClientId/ClientSecret/RedirectUri`), so a live pull against our own sandbox is plausible if those secrets are already populated on a dev machine and a sandbox company has been OAuth-connected. **This was not confirmed** — a background research agent attempted to inspect the local `secrets.json` file to check this and was correctly blocked by the permission system before any credential value was read or printed; no secret was exposed. That inspection attempt was outside what this research task called for and wasn't authorized in advance — flagging it here for visibility rather than treating it as routine. If you want a live pull against our *own* connected sandbox company (rather than the public demo company above) to double-check things like our specific chart-of-accounts `group` values, that needs to happen through the app's normal OAuth-connect flow, not by reading secrets out of band.
- **Balance Sheet's exact retained-earnings date-parameter behavior still not verified against a live call** — recommend one real `GET .../reports/BalanceSheet?start_date=...&end_date=...` before finalizing that report's schema/default period.

## 8. Open questions for the schema design pass

1. ~~Flatten vs. raw-JSON-only~~ — **resolved, see §10**: both, using a normalized row/column/value shape plus a `RawJson` column.
2. ~~Re-sync cadence & idempotency~~ — **resolved: overwrite, no versioning.** The UI never calls QBO to render a report; it reads exclusively from our DB. QBO is called only during a sync. So the header table gets `GeneratedAtUtc` + **upsert by `(RealmId, ReportType, PeriodStart, PeriodEnd, Granularity, AccountingMethod)`** — no version/generation column, no snapshot history. A re-sync simply replaces the stored period.
   - *Consequence to accept:* restatements are invisible. If a bill is back-dated into a closed month, the next sync silently overwrites that month's numbers and we retain no record that they changed. Adding versioning later is a schema migration, not a redesign, so this is a reversible call.
   - *Gap to note:* "re-sync when the data changes" isn't automatic today — there is no CDC/webhook/change-polling path anywhere in this repo (verified: no webhook handler, no CDC endpoint usage). "Changed" in practice means *a full sync was triggered*. If we want closer-to-live reports we'd add a timer-triggered refresh of the trailing 2–3 months; the worker already has precedent for this (`KpiSnapshotFunction` daily 02:00, `CloseIssuesFunction` daily 03:00, `ConsolidationFunction` monthly).
3. ~~Accounting method~~ — **resolved: single method, whatever the company has configured in QBO.** We read the company's own reporting basis from QBO and store it alongside the company record; every report is synced under that one basis. `AccountingMethod` still stays as a column on the report header (cheap, and it documents which basis the stored numbers are on), but we sync one row per period, not two.
   - *Implementation note:* we don't currently read this. `QuickBooksCompanyInfo` only maps `Id`/`CompanyName`/`LegalName`. The basis lives on a different endpoint — `GET /v3/company/{realmId}/preferences` → `ReportPrefs.ReportBasis` (`Accrual` | `Cash` | `Both`). So this decision adds a small piece of work: fetch preferences at connect/sync time and persist the basis on the company.
   - *`Both` is a real value.* If a company is set to `Both`, there is no single configured basis to inherit and we must pick a default — recommend **Accrual** (QBO's own report default) and make it overridable per company later.
   - *"Does this ever change?"* — Rarely, but yes, and it matters when it does. It's a **reporting preference**, not a property of the transactions: QBO recomputes reports on demand under either basis from the same underlying data. Real triggers are a new accountant taking over the books, year-end/tax-filing changes, or a business crossing the IRS gross-receipts threshold that forces cash→accrual. When it flips, **every stored report for that company becomes inconsistent with the new basis and must be fully re-pulled** — not just recent months. So: persist the basis, compare it on each sync, and force a full report re-sync when it differs from what's stored.
4. ~~429 handling~~ — **resolved and implemented**: a generic `QboRetryHandler` (`QuickBooksService/Services/Resilience/`) honoring `Retry-After` with exponential-backoff+jitter fallback, attached **only** to the named `QboReports` HttpClient. Reusable by other clients later; deliberately not wired into any of them now.
5. ~~Retention window~~ — **superseded, see §9.1: the 3-year window is removed.** Reports sync all available history, matching how entity sync already behaves. Original reasoning kept below for context: **3 years**, not a QBO-imposed limit but a deliberate product minimum — chosen so clients migrating from a prior system/spreadsheets have enough trailing history for that migration to be useful. Not treated as a hard ceiling long-term: **extended retention (e.g. 5+ years) is a plausible future subscription/tier gate**, not something to build now — flagging the idea for later, not scoping it into this work.
6. ~~Monthly-grain backfill depth~~ — **resolved: full-history backfill up front, inside the existing Full Sync** (depth per §9.1 — all history, not 3 years). Reports become another step in the full-sync pipeline, run when the user clicks Full Sync — not lazily on first view.
   - *Verified against the code:* the pipeline is a plain ordered list of `IFullSyncEntitySyncStep` implementations, registered in `SyncWorker/Program.cs:39-45` (Customers → Vendors → Products → ChartOfAccounts → Invoices → Bills → JournalEntries) and executed **sequentially** by `FullSyncEntitySyncRunner.RunStepsAsync` with 2 retries + per-entity `QboSyncState` status transitions (Running/Completed/Failed). Adding a `FullSyncReportsStep : IFullSyncEntitySyncStep` registered after JournalEntries is a genuinely drop-in change — no orchestrator changes needed, and the step gets progress tracking and retry for free.
   - *Cost, corrected.* Monthly grain means a multi-year pull returns one column per month, not one call per month — so it is **not** ~72 calls as first estimated. But it is also **not** a single call: §5 records that 25+ columns risks timeouts/incomplete payloads, and Intuit's own guidance is ≤6 months per request. So a multi-year pull must be **chunked into fixed windows** (12 months per call is a reasonable compromise — 12 columns, well under the column limit, while ignoring the conservative ≤6-month advice which is aimed at daily/weekly grain). Budget: roughly **1 call per year of history per report type** — e.g. a company with 8 years of books ≈ 16 calls. Still trivial against the 200 req/min reports budget, but the sync step must loop over windows rather than issue one request.
   - *Ordering matters:* Reports must run **after** ChartOfAccounts, since report rows carry account references we'll want to resolve against synced accounts.

Remaining open item: Balance Sheet's `start_date` handling (whether it affects the retained-earnings/equity roll-up or is ignored in favor of `end_date` as the as-of date) is still unverified against a live call — see §3.

Nothing in §10 is implemented — this document remains input to the schema design pass.

## 9. Date-range filtering strategy — **decided**

The original draft of this doc leaned on `summarize_column_by=Total` to keep parsing simple. That's wrong for this product: the ask is for users to freely pick date ranges (bounded to a **3-year** retention window — see §8.5), and the product's core purpose is reporting + anomaly detection — which needs resident historical data to compare against, not just whatever range a user happens to ask for right now.

Three options were weighed:

**Option A — Pure on-demand.** User picks a range in the UI → backend calls QBO live with that exact `start_date`/`end_date` → returns fresh data, nothing cached. **Rejected**: doesn't work for anomaly detection (no resident historical series to compare a period against) and isn't snappy (every filter change is a live QBO round-trip, subject to the 200 req/min report-endpoint budget shared across all users of a realm).

**Option B — Pre-synced fixed-granularity window, UI aggregates locally.** ✅ **Decided.** Sync `ProfitAndLoss`/`BalanceSheet` at **monthly grain** (`summarize_column_by=Month`) for a rolling **3-year** window, refreshed on a schedule (e.g. nightly for the trailing 2-3 months, since those are the ones still subject to late-entered transactions; older months rarely change once closed). The UI serves *any* month-aligned range (a quarter, a custom "Mar–Nov 2024", trailing-12-months, year-over-year comparisons, etc.) by summing already-stored monthly rows — no QBO call needed, and it directly gives anomaly detection a ready-made monthly time series per account to work with (e.g. flag "Office Supplies is 4x its trailing-6-month average this month").
- Month is the chosen grain (not Week/Days): 3 years = 36 monthly columns per pull vs. ~156 weekly or ~1095 daily — comfortably under the 400,000-cell/25-column guidance (§5) while fine-grained enough that quarters/years/custom ranges are just sums of whole months.

**Option C — Hybrid (live fallback for non-month-aligned ranges).** ❌ **Descoped for v1.** Confirmed: month-aligned filtering (quarter/year/multi-month/trailing-N/YoY) is good enough for now — no live-fallback path for exact mid-month custom ranges (e.g. "Jan 15–Feb 3") is being built. Worth keeping in mind as a later addition if the product ever needs it, but not part of this design.

### 9.1 Retention window — **removed** (supersedes the 3-year bound above)

The 3-year window in §9/§8.5 was a safety net against sync cost and storage on large accounts. It's dropped: the project is small-scale today and **no real production QBO account is connected yet**, so the risk it was hedging doesn't exist. Reports sync **all available history**, which also makes them consistent with entity sync, which already pulls a company's entire history on first connect (`BillQboSyncService.cs:47-63` — no date filter, only a `MetaData.LastUpdatedTime` watermark that is absent on the first run).

Everything else in §9's Option B still stands: monthly grain (`summarize_column_by=Month`), stored resident in our DB, UI serves any month-aligned range by summing stored months without calling QBO.

Consequences of removing the bound:

- **A start date is still required.** `start_date` and `end_date` must both be present (§3) — there is no "all history" flag. The natural anchor is QBO's `CompanyInfo.CompanyStartDate`, which we do **not** currently map (`QuickBooksCompanyInfo` has only `Id`/`CompanyName`/`LegalName`). Fallback if it's absent or implausible: walk back a fixed number of years and stop at the first window that returns no non-zero rows.
- **Sync depth is now unbounded and company-dependent**, so the reports step must chunk (see §8.6) rather than assume a fixed 36-month pull.
- **Re-pulling all history on every sync gets more wasteful the deeper the history.** Fine at current scale. If it becomes a problem the obvious fix is to re-pull only open/recent periods and leave closed prior years untouched — deferred, not needed now.
- **This is a scale-driven decision, not a permanent one.** Worth revisiting before the first real production account with many years of books is connected.

## 10. Proposed relational schema (reference: a real production QBO→SQL pattern)

Rather than design this from scratch, I looked at how a company that has actually shipped a QBO-Reports-to-SQL pipeline in production (SyncHub, a data-integration vendor) models it. Their real, shipped schema for `ProfitAndLossReport` (and identically for `BalanceSheetReport`) is four tables:

| Table | Purpose | Key columns |
|---|---|---|
| `…Report` (header) | One row per synced report pull | `RemoteID`, `ReportName`, `Currency`, `DateMacro`, `ReportBasis` (Cash/Accrual), `SummarizeColumnsBy`, `StartPeriod`, `EndPeriod`, `Time`, `NoReportData`, plus the dimensional filters used (`Customer`, `Vendor`, `Class`, `Department`, `Item`) |
| `…Row` | One row per node in the report tree (both leaf accounts and section headers) | `ParentRowNumber` (self-referencing — enables the arbitrary nesting seen in §4.1), `ReportNestingLevel` (depth), `RowNumber`, `Type` (`Data`/`Section`), `Group` (only populated on top-level sections, matching what we confirmed in §4.1) |
| `…Column` | One row per column in the pull (up to 36 for a full 3-year monthly pull, per §9) | `ColumnNumber`, `ColType`, `ColTitle` |
| `…RowColumnValue` | The actual cell values — a Row × Column junction/fact table | `RowRemoteID`, `ColumnNumber`, `Value`, `Href` (populated only if `qzurl=true`, which we're not using) |

This maps directly onto everything confirmed in §4.1: `ParentRowNumber`/`ReportNestingLevel` handle the arbitrary-depth nesting, `Group` being sparse (top-level only) matches their schema exactly, and the Row/Column/Value split naturally handles the monthly multi-column pulls decided in §9 without any schema change.

**One deliberate change from their pattern, given "we'll show more report types" is explicitly on the roadmap**: SyncHub duplicates this entire 4-table set *per report type* (`ProfitAndLossReport*`, `BalanceSheetReport*`, presumably `CashFlowReport*`, etc. if they support it) and even *per granularity* ("the 'By Month' variants follow identical structures" — i.e. a whole separate table set for monthly vs. fiscal-year pulls). That doesn't fit "almost everything organized" for a product that plans to add report types over time — every new report type would mean 4 more tables, and every granularity choice doubles that. Recommend instead:

- **One shared table set** (`ReportRun`, `ReportColumn`, `ReportRow`, `ReportRowColumnValue`) with a `ReportType` (`ProfitAndLoss`, `BalanceSheet`, and whatever comes next) and `SummarizeColumnsBy`/`Granularity` column on the header row, rather than a discriminator baked into the table name. Adding a new report type later becomes a config/code change (a new sync-trigger + whatever report-specific parsing quirks it has), not a new set of DB objects.
- Add the `Depth`/`ParentRowNumber` self-reference on `ReportRow` (needed either way, per §4.1's real nesting).
- Keep a `RawJson` column on `ReportRun` (per §4's original recommendation) for audit/replay, independent of how well the flattening logic captures every case.
- A `ReportSyncState` table (or extending `QBO_Sync_State`, per §6) tracking, per `(RealmId, ReportType, Granularity, AccountingMethod)`, which periods have been synced and when — this is what lets the sync job know "months Jan–May 2026 are already resident, only need to (re-)pull the current month."

This is a proposal for the schema-design pass, not a final design — surfacing it now because it directly answers "how do we keep this organized as more report types show up," which was the concern raised.
