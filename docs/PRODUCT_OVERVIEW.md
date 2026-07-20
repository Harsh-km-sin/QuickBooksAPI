# Product Overview: CFO Intelligence Platform for QuickBooks Online

This document explains **what this product is**, **which problems it solves**, and **how it helps CFOs, finance leaders, and financial assistants**—including the **QuickBooks sync pipeline**, **analytics & KPIs**, and the **CFO Assistant chatbot**. It reflects the architecture and features described in this repository’s [`README.md`](README.md), deployment guides, and implementation code.

---

## 1. What this product is

This is an **end-to-end financial intelligence platform** built around **QuickBooks Online (QBO)**:

1. **Secure connection** – Users authenticate with QuickBooks via **OAuth 2.0** and connect one or more QBO companies (“realms”). The system stores tokens safely and keeps them refreshed for API calls.

2. **Synchronized accounting data** – Core QBO entities (customers, vendors, products, chart of accounts, invoices, bills, journal entries, etc.) are **pulled into a SQL Server database** and kept in sync through **on-demand syncs** and **background full-company sync**.

3. **Financial warehouse** – Synced data is transformed into **analytics-ready structures** (dimensional / fact style tables) so finance teams can answer questions about **cash, revenue, expenses, profitability, and vendor spend** without exporting spreadsheets from QuickBooks.

4. **CFO-grade analytics** – The API and React UI expose **cash runway**, **vendor spend**, **customer profitability**, **revenue vs expenses**, **KPI snapshots**, **anomaly detection**, **forecasting**, **close & data-quality issues**, and **multi-entity consolidation** (where configured).

5. **CFO Assistant (chatbot)** – A **natural-language layer** sits on top of the same **warehouse-backed metrics**. It uses **rule-based intents** to pull structured numbers from the same services the dashboard uses, then optionally **summarizes** with **Azure OpenAI** using a **strict “no invented numbers”** policy.

---

## 2. Problems this product solves

### 2.1 Fragmented financial visibility

**Problem:** CFOs and assistants often juggle QuickBooks, Excel, email, and ad-hoc reports. Important questions (“How long is our cash runway?”, “Who are our least profitable customers?”, “Where did spend spike?”) take too long to answer.

**How this product helps:** A **single connected system**—sync → warehouse → APIs → UI—so **one source of truth** drives both **operational accounting** (CRUD + sync) and **management reporting** (analytics, KPIs, anomalies, forecasts).

### 2.2 Manual exports and stale data

**Problem:** Exporting from QuickBooks is manual; by the time a deck is built, numbers are stale.

**How this product helps:** **Continuous sync** (per-entity and full-company) plus **scheduled workers** (e.g. KPI snapshots, close-issue scans, consolidation) keep **metrics aligned with books** within the constraints of your deployment (API triggers, Service Bus, timers).

### 2.3 Reactive firefighting instead of early signals

**Problem:** Issues surface late—unexpected vendor spikes, margin pressure, or data-quality gaps discovered at month-end.

**How this product helps:** **Anomaly detection** (post–full-sync pipeline), **KPI history** (sparklines / trends), and **close / data-quality issues** give finance teams **earlier visibility** and a **prioritized work queue** (with resolve flows in the API).

### 2.4 Communication gap between “numbers” and “narrative”

**Problem:** Stakeholders want a **short explanation**, not only a table.

**How this product helps:** The **CFO Assistant** turns **the same underlying metrics** into **concise narrative** (optionally LLM-assisted), with **citations** pointing back to the **API endpoints** that produced the figures—reducing “black box” risk.

---

## 3. Who it is for and how it helps them

### 3.1 Chief Financial Officer (CFO)

| Need | How the product supports it |
|------|------------------------------|
| **Liquidity & runway** | Cash runway analytics (`/api/analytics/cash-runway`), dashboard surfaces, CFO Assistant intents for “runway”, “burn”, “how long”. |
| **Profitability & growth** | Customer profitability, revenue vs expenses series, KPI snapshots (e.g. gross margin, revenue growth, burn multiple—extensible). |
| **Risk & outliers** | Anomaly events after warehouse rebuild; filters by date and severity. |
| **Planning** | Deterministic **forecast scenarios** (base series from warehouse, horizon, assumptions)—API + UI. |
| **Governance of narrative** | CFO Assistant is instructed to **only use supplied data** when Azure OpenAI is enabled; fallback is plain structured text from metrics. |

### 3.2 Financial assistants, controllers, and FP&A

| Need | How the product supports it |
|------|------------------------------|
| **Day-to-day data accuracy** | Entity sync from QBO, list/detail endpoints, CRUD where supported—aligned with QuickBooks as system of record. |
| **Month-end** | Close issues API (e.g. overdue invoices, placeholders for reconciliation/journals/duplicates per roadmap), resolve actions. |
| **Vendor & customer analysis** | Top vendors by spend, vendor spend summary, customer profitability over ranges. |
| **Multi-company view (thin slice)** | Consolidation APIs and entities for parent/child relationships where `dim_entity` / `fact_consolidated_pnl` are populated. |

### 3.3 Operations & leadership (secondary)

Clear **sync status** (queued / running / completed / failed), **correlation IDs** for support, and **health endpoints** help operators run the system reliably alongside QuickBooks.

---

## 4. QuickBooks connection and sync system

Understanding sync is essential: **analytics and the assistant are only as good as the data pipeline.**

### 4.1 OAuth and company context

- Users sign in to **this app** (JWT) and connect **QuickBooks** via the standard **Intuit OAuth** flow (`/api/auth/oAuth`, callback, token storage).
- API calls that touch QBO or company-scoped data require **UserId** and **RealmId** (QuickBooks company id), resolved from JWT claims and typically **`X-Realm-Id`** when a user has multiple companies.

### 4.2 Entity-level sync (API)

For each domain (e.g. customers, invoices), the API exposes patterns such as:

- **List** – Read from the **local SQL** replica (fast UI).
- **Sync** – Pull changes from **QuickBooks Online** into SQL (incremental where sync state allows).

This lets assistants and operators **refresh a slice** without a full run.

### 4.3 Full-company background sync (SyncWorker)

For a **complete refresh**, the system uses **Azure Service Bus** and a separate **SyncWorker** (Azure Functions):

1. The API (e.g. company **full-sync** endpoint) enqueues a **`FullSyncMessage`** to queue **`qbo-full-sync`** (configurable).
2. **SyncWorker** consumes the message, sets **sync status** in SQL (**Queued → Running → Completed / PartiallyFailed / Failed**).
3. **`IFullSyncOrchestrator`** runs **ordered entity steps** (e.g. Customers → Vendors → Products → Chart of Accounts → Invoices → Bills → Journal Entries—see worker registration), with **retries** and **per-entity QBO sync state** updates.
4. After entity sync, the pipeline typically **rebuilds the financial warehouse** and runs **anomaly detection** for that company—so **downstream analytics stay aligned** with the latest sync.

**Operational requirement:** API and worker must share the **same database** and **same Service Bus** configuration; see [`README.md`](README.md) Background Sync Worker section and [`tests/docs/FULLSYNC_WORKER_SMOKE.md`](tests/docs/FULLSYNC_WORKER_SMOKE.md).

### 4.4 Why sync matters for CFO features

- **Warehouse facts** and **KPI snapshots** consume **post-sync** data.
- **Anomalies** run after **warehouse rebuild** in the full-sync path.
- The **CFO Assistant** reads **the same services** that assume **warehouse-backed** or **sync-derived** inputs—so **regular sync** (manual or automated) is part of **trustworthy answers**.

---

## 5. Analytics, KPIs, and intelligence layers

The README describes **phased** delivery; the following is a consolidated view.

### 5.1 Core analytics (Phase 1–style)

- **Financial warehouse** – Dimensional / fact modeling for reporting.
- **Cash runway** – Months of runway, current cash, burn, expected revenue (service + API).
- **Vendor spend** – Top vendors and period summaries.
- **Customer profitability** – Ranked / filtered views over date ranges.
- **Revenue vs expenses** – Monthly series for charts and trends.

### 5.2 Anomalies & KPI engine (Phase 2–style)

- **Anomalies** – Stored events (e.g. vendor spend spike, large transaction, overdue receivables growth—rules evolve). Populated after sync/warehouse steps; exposed via analytics API for dashboards.
- **KPI snapshots** – Periodic computation (e.g. daily timer) into `kpi_snapshot` for metrics such as **GrossMargin**, **RevenueGrowth**, **BurnMultiple** (extensible).  
- **API** – e.g. `GET /api/analytics/kpis` with date range and optional **metric name filters**—powers **sparklines** and trend cards.

### 5.3 Forecasting, assistant, close, consolidation (Phase 3–style)

- **Forecasting** – Create scenarios, deterministic projection, cash/runway implications; `POST` + `GET` forecast APIs.
- **Close & data quality** – Issues table, timer-driven detection, list/filter/resolve APIs.
- **Consolidation** – Parent/child entities and consolidated P&L where schema and workers are populated.

---

## 6. CFO Assistant chatbot: how it works and how it uses KPI-related data

### 6.1 What the chatbot is

- **Endpoint:** `POST /api/cfo-assistant/ask` with a **question** string.
- **Grounding:** The service builds a **`CfoAssistantContext`** by running **intent handlers** that call the **same analytics domain services** as the rest of the app (not a separate shadow database).

### 6.2 Intents implemented (rule-based)

Handlers match keywords in the user’s question (case-insensitive) and append **structured lines** to the narrative plus **citations** (metric name + API path). Examples from the codebase:

| Area | Example triggers (illustrative) | Data source |
|------|-----------------------------------|-------------|
| **Cash runway** | “runway”, “cash runway”, “months of runway”, “how long”, “burn” | `ICashRunwayService` → narrative includes runway months, cash, burn, expected revenue |
| **Revenue vs expenses** | Matched by dedicated handler | `IRevenueExpensesService` |
| **Customer profitability** | e.g. unprofitable / profitability wording | `ICustomerProfitabilityService` |
| **Vendor spend** | Vendor spend oriented questions | `IVendorAnalyticsService` |

If **no** handler matches, the API returns a **helpful default** listing what the assistant can answer (runway, revenue vs expenses, top vendors by spend, customer profitability—per `CfoAssistantService`).

### 6.3 Optional LLM summarization (Azure OpenAI)

When **Azure OpenAI** is configured (`Endpoint`, `ApiKey`, `DeploymentName`):

- The system sends a **system prompt** that instructs the model to behave as a **CFO assistant**, answer **only** from the **provided data**, and stay **concise**.
- If the LLM call fails, the user still receives a **bullet-style summary** from the same **numeric context**.

When **not** configured, answers are **deterministic text** assembled from the metrics.

### 6.4 Relationship to KPI APIs

The **KPI snapshot engine** (`IKpiService`, `/api/analytics/kpis`) is the **dedicated** place for **time-series KPIs** (margins, growth, burn multiple, etc.) and dashboard sparklines.

The **chatbot’s current intent handlers** focus on **runway, revenue/expenses, customer profitability, and vendor spend** as implemented in `Services/CfoAssistant`. For **deep KPI trend questions**, users typically combine:

- **Dashboard / KPI charts** (from KPI API), and  
- **Assistant** for **narrative** on runway, spend, and profitability—**or** future extension to wire `IKpiService` into additional intents if product owners choose to add them.

This separation is worth preserving: **KPIs** remain **auditable, tabular, time-stamped**; the **assistant** adds **interpretation** within **strict grounding rules**.

---

## 7. End-to-end story: from QuickBooks to an answer

1. **Connect QuickBooks** → OAuth tokens stored; realm selected.
2. **Sync data** → Entity syncs and/or **full sync** via Service Bus worker → **SQL** + **warehouse** + **anomalies/KPI jobs** as configured.
3. **Explore** → Dashboards call **analytics APIs** (runway, spend, profitability, KPIs, anomalies, forecasts, close issues).
4. **Ask in natural language** → CFO Assistant resolves **intent** → pulls **same metrics** → optional **LLM** summary → **citations** for traceability.

---

## 8. Deployment and trust (summary)

For **production** CFO use, operators should follow guides such as [`DEPLOYMENT_CFO_PRODUCT_END_TO_END.md`](DEPLOYMENT_CFO_PRODUCT_END_TO_END.md): shared **SQL**, **Service Bus**, **JWT** and **QuickBooks** secrets, optional **Azure OpenAI**, and **CORS** for the React app. **Application Insights** is recommended for tracing sync and API issues.

---

## 9. Summary

| Stakeholder | Value |
|-------------|--------|
| **CFO** | Faster answers on **liquidity**, **profitability**, **risk**, and **planning**, with optional AI narrative **tied to real metrics**. |
| **Financial assistants** | Less manual export work; **synced books**, **close-quality** signals, and **repeatable** analytics APIs. |
| **Organization** | **One pipeline** from **QuickBooks** → **database** → **warehouse** → **analytics & assistant**, with **background sync** and **observable** status for operations. |

This product is best understood as **QuickBooks-connected financial operations plus a CFO intelligence layer**: sync and warehouse make the numbers **trustworthy**; analytics, KPIs, anomalies, and forecasts make them **actionable**; the CFO Assistant makes them **accessible in conversation**—without replacing the **system of record** (QuickBooks) or the **auditability** of structured APIs.
