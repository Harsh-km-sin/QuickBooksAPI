# AI-First GL Analysis — Complete Feature Breakdown
### Everything You Need to Know: Product, Technical, UX, Dev Workflow, and Team Setup

> **Project:** SyncTools / Accounting Automation Platform  
> **Team:** 2 developers, parallel builds using different frameworks  
> **AI Model:** Claude (Sonnet / claude-sonnet-4-6)  
> **Status:** New feature — greenfield module added to existing product  

---

## Part 1: What Is This Feature, in Plain English?

### The Problem It Solves

Right now, accountants and auditors who use SyncTools spend **hours — sometimes entire days** — manually reviewing a company's General Ledger (GL) for a given month or year. The GL is the master record of every financial transaction a business makes: payments in, payments out, journal entries, adjustments. Reviewing it means checking thousands of rows for mistakes, fraud signals, unusual patterns, and policy violations — by hand, in spreadsheets.

This is slow, error-prone, and scales badly. An accountant managing 10–20 client companies has to repeat this process for every one of them, every month.

### What This Feature Does

**GL Review (GL Analyzer)** is an AI-powered module that automates the heavy lifting of a full general ledger review. The accountant uploads their GL data (CSV or Excel file), the system analyzes every single transaction automatically, scores each one for risk, flags the suspicious ones, and presents everything in an interactive dashboard. The accountant's job shifts from "find the problems" to "review what the AI found and decide what to do about it."

The full vision goes further: the AI explains in plain English *why* each transaction was flagged, lets the user ask follow-up questions in natural language ("show me all manual journal entries over $10K posted after hours"), and generates a complete, formatted report the accountant can hand to an auditor.

### What Makes It Different from Existing Tools

- **MindBridge** (the market leader) is an enterprise product costing $50K+/year, designed for Big 4 audit firms with professional onboarding. It requires manual CSV uploads or ERP integrations.
- **Our GL Review** is designed for SMBs and mid-market accounting firms using QuickBooks Online, Xero, or Zoho Books. It's zero-config, conversational, and SaaS-priced — and because SyncTools already has OAuth connections to those platforms, future versions can pull GL data automatically with one click.

### Where It Lives in the App

SyncTools currently has: Home, Sales Orders, Payouts, Reconciliation (per e-commerce channel), and similar sections. GL Review becomes a **new top-level sidebar item** — it is NOT a sub-feature of Reconciliation. That is a deliberate product decision. Reconciliation in SyncTools means "did e-commerce orders match what we sent to QuickBooks?" GL Review means "is the actual ledger of this company accurate, consistent, and free of red flags?" These are different jobs.

---

## Part 2: Who Uses It and Why They Care

| Persona | What They Do | What They Need from GL Review |
|--------|--------------|-------------------------------|
| **Accountant** (primary) | Runs GL reviews for 1–many clients monthly | Complete the review in minutes, not hours. Peace of mind nothing was missed. |
| **Finance Manager** (primary) | Oversees GL review across entities | Same as accountant, plus visibility across companies. |
| **CFO / Leadership** (buyer) | Delegates accounting and review work | Confidence books are accurate. High-level summary without reading every transaction. |
| **Auditor** (ideal customer) | Reviews and signs off on financials | Consistent, documented, AI-assisted review. Audit-ready report. |

---

## Part 3: The Full Feature — All Pages, All Concepts

### The Menu (8 Sections)

```
GL Review (sidebar)
├── Dashboard
├── Transaction Explorer
├── Anomaly Analysis
├── Period Comparison
├── Trend Analysis
├── AI Assistant
├── Reports
└── Settings
```

---

### Page 1: Dashboard

**What it is:** The first thing the accountant sees after analysis completes. A high-level health check of the entire GL.

**What's on it:**
- **4 summary metric cards:** Total Transactions, Flagged for Review, Critical/High Risk count, Average Risk Score — each with a directional change indicator (up/down arrow + %)
- **AI Executive Summary:** A 3–5 paragraph AI-written narrative in plain English. Example: *"Analysis of Q4 2025 for Acme Corp identified 3 critical and 14 high-risk transactions requiring immediate attention. The most significant finding is a cluster of 7 manual journal entries to Revenue accounts posted on December 31st by a user who has not previously posted to Revenue..."*
- **Risk Distribution Chart:** Pie or donut chart showing how many transactions fall into each risk tier (Critical / High / Medium / Low / Normal). Click a slice to drill into that tier.
- **Top 10 Flagged Transactions:** The highest-risk entries listed inline, with one-line AI explanations.
- **12-Month Trend Sparklines:** Mini line charts showing transaction volume and flagged counts over time.
- **Period Comparison Card:** If prior-period data exists, shows delta in risk profile, new risk patterns, and resolved issues.

---

### Page 2: Transaction Explorer

**What it is:** The full data table. Every single transaction in the GL, scored and filterable.

**What's on it:**
- Full searchable, sortable, paginated table of all GL transactions
- **Columns:** Date, Account, Entity (Vendor/Customer), Amount, Source Type (Manual JE / Invoice / System), Risk Score, Risk Tier, Anomaly Flags
- **Filters:** Risk tier, account, entity, date range, anomaly type, amount range, source type, created-by user
- **Expandable rows:** Click any row to reveal:
  - AI explanation of why this was flagged
  - All triggered anomaly flags with individual sub-scores
  - Monetary flow context (what debit/credit lines are paired with this entry)
  - Suggested investigation steps ("Verify with [User] that this entry has supporting documentation")
- **Click-through actions:** Human review actions — mark as reviewed, flag for follow-up, reject AI suggestion

---

### Page 3: Anomaly Analysis (Deep Dive)

**What it is:** Individual views per anomaly detection type, with dedicated visualizations for each.

**The anomaly detection views:**

**Benford's Law View**
> Benford's Law states that in naturally occurring numerical datasets, the first digit is "1" about 30% of the time, "2" about 18%, and so on down to "9" at ~5%. When humans fabricate or manipulate numbers, they don't follow this distribution naturally — their first-digit choices cluster differently. This is a well-established fraud detection technique used by the IRS.
- Chart: Expected digit distribution vs. actual distribution per account type
- Deviations highlighted in red

**Monetary Flow Map**
> Every accounting entry is a debit AND a credit to different accounts. The combination of which account is debited and which is credited tells you the "flow" of money. In a normal business, you see predictable patterns: cash hits the bank account when a customer invoice is paid. Unusual combinations — cash flowing to an account it has never gone to before — is a signal worth investigating.
- Network graph: accounts as nodes, transactions as edges
- Rare/unusual flows highlighted in red
- Click any flow to see all transactions in it

**Timeline Heatmap**
- Calendar view showing transaction volume and risk intensity by day
- Weekend/holiday entries visually obvious
- Month-end, quarter-end spikes visible at a glance

**Amount Distribution**
- Histogram of transaction amounts per account
- Outliers marked
- Threshold-testing patterns visible (e.g., many entries just below $5,000 if $5K requires manager approval)

**Entity Risk Matrix**
- Scatter plot: vendors/customers by transaction volume (x) vs. average risk score (y)
- High-volume + high-risk entities in the danger quadrant (top right)

---

### Page 4: Period Comparison

**What it is:** Current period vs. prior period, side by side.

**What it shows:**
- New accounts used in current period (never used before)
- Dormant accounts reactivated
- Changes in transaction volume per account
- Shifts in risk score distribution
- New entity relationships that didn't exist in prior period
- Changes in monetary flow patterns

---

### Page 5: Trend Analysis

**What it is:** Time-series view across many months.

- Account balance trends decomposed into trend + seasonality + anomaly
- Flags abnormal deviations from expected seasonal patterns
- Track which accounts are trending upward/downward in risk over time

---

### Page 6: AI Assistant (Chat)

**What it is:** A conversational chat interface embedded in the GL module, powered by Claude.

**What you can ask:**
- "What are the top 3 risk areas this month?"
- "Show me all manual journal entries over $10,000 posted after business hours"
- "Why was this vendor flagged?"
- "Compare this quarter's expense patterns to last quarter"
- "Generate an audit memo for the revenue account anomalies"

The AI has full context of the current analysis run. It can answer in natural language, generate tables on the fly, and explain findings.

---

### Page 7: Reports

**What it is:** Auto-generated, export-ready reports.

**Report types:**
- **Executive Summary Report (PDF):** CFO-ready, narrative + key findings, 2–5 pages
- **Detailed Anomaly Report:** All flagged transactions with AI explanations, supporting charts
- **Risk Score Distribution Report:** Full scoring breakdown
- **Period Comparison Report:** Current vs. prior delta analysis
- **Custom filtered exports:** CSV/Excel of any filtered view

All reports include AI-generated narrative sections alongside data tables and charts.

---

### Page 8: Settings

- Configurable risk thresholds (e.g., what score = "Critical")
- Rule configuration (approval thresholds, account-specific rules)
- Sync schedule (post-v1: automated monthly triggers)
- Upload template download

---

## Part 4: The AI Engine — How It Actually Works

The system uses a **4-tier ensemble analysis approach**. Think of it like multiple specialists reviewing the same data independently, then combining their votes.

### Tier 1: Statistical Analysis (Fast, Deterministic)
Runs first, on every transaction, in milliseconds. These are mathematical rules that always produce the same answer.

| Method | What It Catches |
|--------|----------------|
| **Benford's Law** | Fabricated or manipulated number distributions |
| **Z-Score Outlier Detection** | Amounts that deviate >2σ or >3σ from the account's mean |
| **Round Number Analysis** | Suspicious concentration of round amounts ($5,000.00 exactly) |
| **Threshold Breach Detection** | Entries just below approval thresholds ($4,999 when $5K needs approval) |
| **Duplicate Detection** | Exact and near-duplicates (same amount + entity within N days) |
| **Weekend/Holiday Posting** | Entries created when business is normally closed |
| **Backdating Detection** | Large gap between created_date and transaction_date |
| **Period-End Clustering** | Unusual spikes of journal entries at month/quarter/year-end |

### Tier 2: Machine Learning (Pattern-Based)
Runs after Tier 1. Unsupervised models that learn from the data itself — no predefined rules.

| Model | What It Catches |
|-------|----------------|
| **Isolation Forest** | Multi-dimensional outliers across amount + frequency + timing + entity |
| **DBSCAN Clustering** | Transactions that don't fit any known cluster ("noise points") |
| **Autoencoder Neural Net** | Complex, non-linear anomalies across many features at once |
| **Time-Series Decomposition** | Abnormal residuals that break expected seasonal patterns |
| **Association Rule Mining** | Unusual account pairings (rare monetary flows) |
| **Graph Network Analysis** | Circular flows, hidden entity connections |

### Tier 3: Business Rules Engine (Configurable)
User-configurable rules encoding domain expertise.

- Manual JEs over a configurable amount → flag
- Same user creates and approves entries → segregation of duties violation
- New vendor with large first transaction → flag
- Payment to vendor without corresponding purchase order → flag
- Debit to a revenue account → flag (usually wrong direction)
- Suspense account not cleared within N days → flag

### Tier 4: LLM Reasoning — Claude (The AI-First Differentiator)
After Tiers 1–3 produce flags and scores, Claude analyzes the flagged clusters and generates:

1. **Natural language explanations** for each high-risk item — not just "Z-score outlier" but *why that matters in context*
2. **Contextual risk assessment** — one flag may be benign, three flags on the same transaction elevates risk significantly
3. **Suggested investigation steps** — concrete next steps written for the accountant
4. **Executive summary** — the full narrative report
5. **Conversational drill-down** — the AI Assistant chat

### Risk Scoring

Every transaction gets a composite score from 0.0 to 1.0:

| Tier | Weight | Reason |
|------|--------|--------|
| Tier 1 Statistical | 25% | Precise, deterministic, well-understood |
| Tier 2 ML | 35% | Broadest coverage, catches hidden patterns |
| Tier 3 Business Rules | 25% | Domain expertise, highly interpretable |
| Tier 4 LLM | 15% | Holistic contextual reasoning |

| Score | Risk Tier | Action |
|-------|-----------|--------|
| 0.85–1.00 | **Critical** | Immediate investigation; notify CFO |
| 0.65–0.84 | **High** | Review within 48 hours; documented sign-off |
| 0.40–0.64 | **Medium** | Review during normal audit cycle |
| 0.15–0.39 | **Low** | Monitor |
| 0.00–0.14 | **Normal** | No action needed |

---

## Part 5: Technical Architecture (How It's Built)

### Existing Platform Context
- **Frontend:** React (existing SyncTools UI with existing auth/layout)
- **Backend:** .NET (existing SyncTools API)
- **Database:** Azure SQL Database (already in use)
- **File Storage:** Azure Blob Storage
- **Queue:** Azure Service Bus

GL Review is a **new module added on top of this existing infrastructure.** It does not replace or break anything that exists.

### V1 Data Flow (Simplified — Manual Upload)

```
User uploads CSV/Excel
        ↓
.NET API accepts file → Azure Blob Storage (stored indefinitely)
        ↓
.NET validates & parses → normalizes to canonical GL schema
        ↓
.NET persists run metadata + normalized rows → Azure SQL
        ↓
.NET enqueues job → Azure Service Bus
        ↓
Python FastAPI worker picks up job → runs Z-Score analysis
        ↓
Python writes results (scores + flags) → Azure SQL
        ↓
.NET updates run status → "Complete"
        ↓
Frontend polls → fetches results → renders Transaction Explorer
        ↓
User reviews, takes actions, exports report
```

### Full Architecture (Post-V1)

```
┌─────────────────────────────────────────────────────┐
│                  React Frontend                      │
│  Dashboard / Transaction Explorer / AI Chat / etc.  │
└────────────────────────┬────────────────────────────┘
                         │ REST API
┌────────────────────────▼────────────────────────────┐
│              .NET Backend (Orchestrator)             │
│  Auth │ File Upload │ Blob Write │ SQL │ Queue       │
└─────┬──────────────────────────────────┬────────────┘
      │ Azure Service Bus                │ Azure SQL
┌─────▼──────────────────┐    ┌──────────▼────────────┐
│  Python FastAPI Worker  │    │   Azure SQL Database  │
│  Tier 1: Statistics     │    │  GL_Runs              │
│  Tier 2: ML Models      │    │  GL_Transactions      │
│  Tier 3: Rules Engine   │    │  GL_Results           │
│  Tier 4: Claude API     │    │  GL_Reports           │
└────────────────────────┘    └───────────────────────┘
```

### Canonical GL Schema (The Normalized Format)
Every uploaded file gets mapped to these fields before analysis:

| Field | Type | Purpose |
|-------|------|---------|
| transaction_id | UUID | Unique identifier |
| journal_entry_id | String | Groups debit/credit lines |
| transaction_date | Date | When posted |
| created_date | DateTime | When created (for backdating detection) |
| account_id / account_name | String | Chart of accounts reference |
| account_type | Enum | Asset / Liability / Equity / Revenue / Expense |
| posting_type | Enum | Debit or Credit |
| amount | Decimal | Transaction amount (always positive) |
| entity_type / entity_name | String | Vendor / Customer / Employee |
| description | String | Transaction memo |
| source_type | Enum | Manual JE / Invoice / Bill / System-generated |
| created_by | String | Who posted it |
| composite_risk_score | Float 0–1.0 | *Computed by AI Engine* |
| risk_tier | Enum | *Computed* |
| anomaly_flags[] | Array | *Computed* |
| ai_explanation | String | *LLM-generated* |
| suggested_action | String | *LLM-generated* |

### Technology Stack (Full Vision)

| Component | Tool |
|-----------|------|
| Frontend | React + Recharts / Chart.js + D3.js (network graphs) |
| Backend Orchestration | .NET (existing) |
| Analysis Engine | Python FastAPI |
| Statistical Analysis | statsmodels, scipy |
| ML Models | scikit-learn (Isolation Forest, DBSCAN), PyTorch (autoencoder) |
| LLM | Claude API (claude-sonnet-4-6) — Tier 4 reasoning + AI Chat |
| Database | Azure SQL Database |
| File Storage | Azure Blob (indefinite retention) |
| Job Queue | Azure Service Bus |
| Report Generation | Puppeteer (PDF) + docx library (Word) |
| Caching | Redis (post-v1) |

---

## Part 6: V1 Scope — What Gets Built First

**V1 is deliberately narrow.** The philosophy is: one robust cycle end-to-end is better than many half-built tiers.

### V1 Must-Haves
- User uploads CSV or Excel file with GL data
- System validates the file format and required columns
- System runs **Z-Score outlier detection** (Tier 1 only) on every transaction
- Results stored and served to the frontend
- User sees a Transaction Explorer with flagged transactions
- User can filter, drill into, and review flagged items
- User can export a report (PDF or CSV of flagged items)
- Performance: **10,000 transactions processed within 3–5 minutes** end-to-end

### V1 Explicitly Out of Scope
- Live QBO / Xero / Zoho API connections for this module
- Tier 2 (ML models), Tier 3 (rules engine), Tier 4 (LLM / Claude)
- AI Chat / AI Assistant page
- Voice-to-rules feature
- Scheduled / email-triggered runs
- Period comparison, trend analysis, graph network views
- Multi-currency normalization

### V1 Upload Template
A canonical CSV/Excel column format will be documented and provided as a download. Required columns for Z-Score analysis:
- `transaction_date`
- `account_id` or `account_name`
- `posting_type` (Debit/Credit)
- `amount`

Optional (enhance analysis if present): `created_date`, `entity_name`, `description`, `source_type`

---

## Part 7: Phased Delivery Plan (Full Roadmap)

| Phase | Weeks | What Gets Built |
|-------|-------|----------------|
| **Phase 1: Foundation** | 1–6 | Upload flow, canonical schema, Z-Score Tier 1, basic Transaction Explorer, simple dashboard. **V1 ships here.** |
| **Phase 2: Intelligence** | 7–12 | Tier 2 ML (Isolation Forest + DBSCAN), monetary flow analysis, composite Tier 1+2 scoring, anomaly deep-dive views, period comparison. |
| **Phase 3: AI-First** | 13–18 | Claude API integration (Tier 4), AI Chat, auto-generated executive summary + reports, Tier 3 business rules engine, full 4-tier composite scoring. |
| **Phase 4: Polish & Scale** | 19–24 | Trend analysis, graph network analysis, autoencoder model, performance tuning for large datasets, scheduled analysis + email alerts, QBO/Xero live connection. |

---

## Part 8: User Stories Summary (30 Stories, US-001 – US-092)

The user stories are organized into these groups:

| Group | Stories | What They Cover |
|-------|---------|----------------|
| Navigation & Shell | US-001–003 | Sidebar access, company/period selector, file upload modal |
| Dashboard | US-010–016 | Metric cards, AI summary, risk chart, trend chart, top flagged list, period card, quick actions |
| Transaction Explorer | US-020–027 | Full table, filters, row drill-down, AI explanation, monetary flow, actions, export |
| Anomaly Analysis | US-030–035 | Benford's Law view, flow map, timeline heatmap, amount distribution, entity matrix |
| Period Comparison | US-040–044 | New/dormant accounts, volume changes, risk shifts, entity changes |
| AI Assistant | US-050–055 | Chat interface, predefined prompts, follow-up questions, report generation from chat |
| Reports | US-060–066 | Executive PDF, detailed anomaly report, risk distribution report, custom export, scheduled reports |
| Settings | US-070–074 | Threshold config, rule config, upload template, sync schedule |

---

## Part 9: Acceptance Criteria Summary (30 AC Blocks, AC-001 – AC-092)

Each user story has a corresponding AC block. Key patterns:

- **Navigation:** GL Review sidebar item visible to all authenticated users, 7 sub-items, auto-expand on active route
- **Upload modal:** Drag-and-drop, CSV/XLSX/XLS, non-blocking (pages accessible without upload), async processing trigger
- **Dashboard cards:** 4-col → 2-col → 1-col responsive grid, loading spinner, null state
- **Transaction table:** Pagination, all filters functional, expandable rows, click-through actions
- **Exports:** PDF includes AI narrative; CSV includes all columns; correct MIME types; download filenames include company + period
- **Performance:** Results available within 3–5 minutes of upload for 10K row files

---

## Part 10: For 2 Developers Using Different Frameworks with Claude Code

### The Setup
Two developers, same feature, different frameworks — a framework comparison experiment using Claude Code as the AI coding assistant. Here's how to think about the division of work and which frameworks to consider.

### Recommended Framework Pairings

#### Developer A: T3 Stack (TypeScript-first, full-stack)
- **Frontend:** Next.js (React) with TypeScript
- **Backend:** tRPC (type-safe API layer) + Prisma ORM
- **Database adapter:** Prisma pointing at Azure SQL
- **Queue:** Azure Service Bus SDK for Node.js
- **Analysis:** REST calls to Python FastAPI (analysis engine stays Python regardless)
- **Why it works with Claude:** Claude generates excellent TypeScript, tRPC schemas, and Prisma models. The end-to-end type safety means Claude's code compiles and is correct more often.

#### Developer B: GSD Stack (Pragmatic, ship-fast)
- **Frontend:** React + Vite (no framework overhead)
- **Backend:** Express.js or Fastify (Node.js, plain REST)
- **Database adapter:** raw SQL or Drizzle ORM
- **Queue:** Azure Service Bus SDK
- **Analysis:** REST calls to Python FastAPI
- **Why it works with Claude:** Simpler structure means Claude can scaffold a full endpoint in one shot. Fewer abstractions = fewer Claude errors from misunderstood conventions.

#### Alternative for Developer B: Laravel + Inertia (PHP)
If the team has PHP experience, Laravel with Inertia.js (React frontend, Laravel backend) is extremely Claude-friendly because Laravel conventions are heavily represented in training data — Claude writes excellent Laravel.

### What Both Developers Share (Non-Negotiable Shared Layer)
- **Python FastAPI analysis engine** — same code, used by both
- **Azure SQL schema** — same tables, both point at same (or mirrored) DB
- **Azure Blob** — same storage strategy
- **Azure Service Bus** — same queue topology
- **Canonical GL schema** — same column definitions

### Claude Code Workflow Recommendations

**Use TDD (Test-Driven Development)**
Claude Code is excellent at writing tests first and then implementation. For GL analysis, start every analysis method with a test:
```
Prompt: "Write a pytest test for Z-Score outlier detection on GL transaction data. 
The function takes a list of {account_name, amount} dicts and returns each row 
with a z_score and is_outlier boolean (outlier if |z| > 2.5)"
```
Then: "Now implement the function that passes these tests."

**Use GSD (Get Stuff Done) Methodology**
For v1, the GSD approach is: define the simplest possible version of each feature that works end-to-end, build it, ship it, iterate. Tell Claude Code:
- What the input is
- What the output must be
- What the acceptance criteria say
- "Keep it simple, no over-engineering, this is v1"

**Prompt Strategy for GL Analysis with Claude Code**
1. Feed Claude the canonical schema first (paste the field table from Part 5)
2. Feed Claude the acceptance criteria for the specific story you're building
3. Ask for one component at a time, not the whole page
4. For the Python analysis engine: always start with "write the analysis function with full type hints and docstrings"

**Parallel Development Split Suggestion**

| Week | Developer A | Developer B |
|------|------------|------------|
| 1–2 | Upload flow + file validation (frontend + .NET API) | Python FastAPI setup + Z-Score algorithm (TDD) |
| 3–4 | Azure Blob storage + Azure Service Bus job queue | Canonical schema normalization + Azure SQL tables |
| 5–6 | Transaction Explorer page (table, filters, pagination) | Dashboard page (metric cards, risk chart) |
| Post-V1 | ML tier integration (Isolation Forest) | Claude API integration (Tier 4 explanations) |

---

## Part 11: Pages Count and Page-by-Page Summary

### How Many Pages?

**V1 ships:** 3–4 functional pages  
**Full product:** 8 pages + modal overlay

| # | Page | V1? | Complexity |
|---|------|-----|------------|
| 1 | **Dashboard** | Partial (basic) | High — multiple widgets, AI narrative, charts |
| 2 | **Transaction Explorer** | ✅ Full | Very High — table, filters, drill-down, actions |
| 3 | **Anomaly Analysis** | ❌ Post-v1 | High — 5 different visualization types |
| 4 | **Period Comparison** | ❌ Post-v1 | Medium — side-by-side data comparison |
| 5 | **Trend Analysis** | ❌ Post-v1 | Medium — time-series charts |
| 6 | **AI Assistant** | ❌ Post-v1 | High — Claude API chat, streaming, context mgmt |
| 7 | **Reports** | Partial (basic export) | Medium — report generation, download |
| 8 | **Settings** | Partial (upload template) | Low — forms, threshold config |
| — | **Upload Modal** | ✅ Full | Medium — drag-drop, validation, async trigger |

---

## Part 12: Open Questions Still to Decide

These are the unresolved items from the PRD and spec that the team needs to answer before or during development:

1. **Export format for v1:** Is the v1 report a full PDF with AI narrative, or just a CSV of flagged rows? PRD says "complete report" — clarify what "complete" means without Tier 4 LLM.
2. **Column mapping UX:** For uploads where column names don't match the canonical schema, is there a UI for mapping, or does v1 enforce a strict template only?
3. **Python worker hosting:** Same Azure App Service as .NET? Separate Azure Functions? Azure Container Instance? Decide before infrastructure setup.
4. **Azure Service Bus topology:** Single queue for all GL jobs, or one per job type? Recommend: single queue, job type in message body.
5. **LLM provider confirmation:** The spec recommends Claude. This is confirmed by the project context (Claude Code + Claude API). Use `claude-sonnet-4-6`.
6. **Historical data depth:** For period comparison (Phase 2), how many months of history should the upload template support? Recommend: minimum 12 months, ideal 24.
7. **Multi-currency:** Not in v1 scope, but the upload template should include a `currency` column now so the data is captured even if not yet used.
8. **Pricing model:** Per-company, per-analysis, or tiered by transaction volume — needs a decision before Phase 3 ships publicly.

---

## Part 13: Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| **10K rows doesn't complete in 3–5 min** | Medium | High | Build async (Service Bus), profile Z-Score early, set user expectation with progress indicator |
| **Upload format too rigid** | High | Medium | Provide template download, allow optional column mapping in v1 |
| **Scope creep into Tier 2/3/4 in v1** | High | High | Hard v1 scope lock: one analysis type, upload only |
| **.NET ↔ Python reliability** | Medium | High | Use Service Bus (async, retryable), dead-letter queue, run status polling |
| **Claude API cost at scale** | Low (v1) | Medium | Tier 4 is post-v1; when adding, batch LLM calls per run, not per transaction |
| **Two devs diverging on architecture** | Medium | Medium | Shared Python engine, shared DB schema, weekly sync on interface contracts |
| **Data sensitivity / GL exposure** | Low | High | Azure encryption at rest + in transit, org-scoped access, existing SyncTools auth |

---

## Part 14: Key Concepts Glossary

| Term | Meaning in This Project |
|------|------------------------|
| **GL** | General Ledger — the master record of all financial transactions for a company |
| **Journal Entry (JE)** | An accounting record that adjusts account balances; always has a debit and a credit |
| **Debit / Credit** | In accounting: debit increases assets/expenses, credit increases liabilities/equity/revenue |
| **Canonical Schema** | The unified data format all uploads are normalized into before analysis |
| **Monetary Flow** | The combination of which account is debited and which is credited in a paired entry |
| **Risk Tier** | Critical / High / Medium / Low / Normal — derived from composite risk score |
| **Composite Score** | Weighted average of signals from all 4 analysis tiers (0.0 – 1.0) |
| **Z-Score** | A statistical measure of how many standard deviations a value is from the mean |
| **Benford's Law** | Statistical law about first-digit frequency in naturally occurring data; used for fraud detection |
| **Isolation Forest** | An ML algorithm that detects anomalies by randomly partitioning data |
| **DBSCAN** | A density-based clustering algorithm; flagging "noise points" = unusual transactions |
| **Tier 4 / LLM** | The Claude API layer that generates natural language explanations and the AI Chat |
| **Ensemble** | Combining multiple analysis methods and weighting their outputs into one score |
| **Period-End Clustering** | Suspicious spike of JEs right before month/quarter/year close — a fraud signal |
| **Backdating** | When `created_date` and `transaction_date` have a large gap — possible retroactive manipulation |
| **Segregation of Duties** | Accounting principle: the person who creates an entry shouldn't also approve it |

---

*Document generated from: PRD-AI-First-GL-Analysis.md, AI-First-GL-Analysis-Specification.md, TRD-AI-First-GL-Analysis.md, user-stories.md, acceptance-criteria.md*  
*Satva Solutions / SyncTools — Internal / Confidential*
