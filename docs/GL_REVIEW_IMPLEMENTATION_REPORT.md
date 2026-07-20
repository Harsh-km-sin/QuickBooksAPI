# GL Review Feature — Implementation Report (feat/GL-review)
**Journal Entry Analysis & Anomaly Detection System**

> **Document purpose:** AI-ready, exhaustive technical reference for the GL Review feature as implemented on the `feat/GL-review` branch.  
> **Branch:** `feat/GL-review` · submodule path: `accounting-automations/accounting-automations/`  
> **Last commit:** `238c9eb8` — feat(gl-review): complete GL Review feature — 4-tier anomaly detection, LLM narratives, full UI  
> **Date scanned:** 2026-06-24

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Key Differences vs Prior Implementation](#2-key-differences-vs-prior-implementation)
3. [System Architecture Overview](#3-system-architecture-overview)
4. [Database Schema](#4-database-schema)
5. [File Parsing & Normalization](#5-file-parsing--normalization)
6. [Four-Tier Detector Framework](#6-four-tier-detector-framework)
   - 6.1 [Tier 1 — Statistical & Rule-Based (32 Detectors)](#61-tier-1--statistical--rule-based-32-detectors)
   - 6.2 [Tier 2 — Machine Learning (7 Detectors)](#62-tier-2--machine-learning-7-detectors)
   - 6.3 [Tier 3 — Business Rules Engine](#63-tier-3--business-rules-engine)
   - 6.4 [Tier 4 — LLM Narrative Scoring](#64-tier-4--llm-narrative-scoring)
7. [Risk Score Fusion & Tier Classification](#7-risk-score-fusion--tier-classification)
8. [Worker Execution Pipeline](#8-worker-execution-pipeline)
9. [Settings & Configuration System](#9-settings--configuration-system)
10. [Backend API Routes](#10-backend-api-routes)
11. [Analytics & Reporting](#11-analytics--reporting)
12. [Notification System (Email + Slack)](#12-notification-system-email--slack)
13. [Frontend Architecture](#13-frontend-architecture)
    - 13.1 [Pages](#131-pages)
    - 13.2 [Components & Context](#132-components--context)
14. [Type Contracts](#14-type-contracts)
15. [Migration History](#15-migration-history)
16. [End-to-End Data Flow](#16-end-to-end-data-flow)
17. [Key Design Decisions & Rationale](#17-key-design-decisions--rationale)
18. [Complete File Index](#18-complete-file-index)

---

## 1. Executive Summary

The **GL Review** feature is a production-grade, multi-tiered General Ledger (GL) auditing system that ingests journal entry exports from any ERP (QuickBooks, SAP, NetSuite, Xero) and surfaces anomalous or suspicious entries for auditor review using layered detection across statistics, machine learning, user-defined rules, and large language models.

### Core Capabilities

| Capability | Description |
|---|---|
| **File Ingestion** | CSV, XLSX, XLS with auto-column detection across naming variations |
| **Tier-1 Statistical** | 32 detectors: duplicates, Z-Score, Benford's Law, backdating, period-end clustering, keyword flags, behavioral patterns, and more |
| **Tier-2 ML** | 7 detectors: Isolation Forest, DBSCAN, Association Rules (Apriori), COPOD, ECOD, account profiling, entity profiling |
| **Tier-3 Business Rules** | Unlimited user-defined rules with field-based and threshold conditions, severity mapping |
| **Tier-4 LLM** | AI narrative explanations for critical/high entries via Claude, GPT, or Gemini — story-first plain English, capped at 100 entries/run |
| **Configurable Weights** | All four tier weights and per-detector enable/disable flags are user-configurable from the Settings UI |
| **Feedback Loop** | Per-entry auditor verdicts with resolution status (confirmed TP / dismissed FP / escalated) |
| **AI Chat** | Multi-turn assistant per run with persistent `gl_chat_messages` history |
| **Analytics** | Period metrics, entity risk, anomaly breakdowns, Benford charts, score distribution |
| **Scheduled Reports** | PDF/XLSX/CSV report generation on daily/weekly/monthly schedules |
| **Notifications** | Email (SMTP) and Slack webhook alerts on run completion |

### Technology Stack

| Layer | Technology |
|---|---|
| Frontend | React + TypeScript, Recharts, TanStack Query |
| Backend | Node.js + Express, TypeScript |
| Database | PostgreSQL (Drizzle ORM) |
| Queue | Bull (Redis), in-process fallback |
| AI/LLM | Anthropic Claude (default), OpenAI GPT, Google Gemini |
| ML | Pure TypeScript: Isolation Forest, DBSCAN, Apriori, COPOD, ECOD |
| File Parsing | `xlsx` library (auto-detects delimiter for CSV) |
| Schema Validation | Zod |
| Notifications | SMTP Email + Slack Incoming Webhooks |
| Reports | PDF generation + XLSX + CSV |

---

## 2. Key Differences vs Prior Implementation

The `feat/GL-review` branch is a substantially more complete implementation. The following differences are the most significant:

| Dimension | Previous (earlier branch) | Current (`feat/GL-review`) |
|---|---|---|
| **Table names** | `gl_review_runs`, `gl_review_transactions`, `gl_review_results`, `gl_review_rules`, `gl_review_feedback`, `gl_review_features` | `gl_runs`, `gl_entries`, `gl_anomalies`, `gl_settings`, `gl_business_rules`, `gl_feedback`, `gl_chat_messages`, `gl_scheduled_reports` |
| **Score range** | 0.0–1.0 float | 0–100 integer |
| **Score formula** | Weighted Noisy-OR (`1 − ∏(1 − wᵢsᵢ)`) | Weighted average + `SCORE_AMPLIFIER = 3.5` |
| **Risk tier thresholds** | 0.85 / 0.65 / 0.40 / 0.15 (fixed) | 65 / 40 / 20 / 8 (user-configurable) |
| **Tier-1 detectors** | ~15 | **32 named detectors** |
| **Tier-2 ML detectors** | 3 (Isolation Forest, DBSCAN, Apriori) | **7** (adds COPOD, ECOD, account behavior, entity behavior) |
| **Tier weights** | Implicit (detector weights summed) | **Explicit 4-tier weights** (T1=0.25, T2=0.35, T3=0.25, T4=0.15, all configurable, auto-normalize to 1.0) |
| **LLM scope** | All flagged rows (best-effort) | Only critical+high entries, **max 100/run**, GST/tax entries excluded |
| **LLM providers** | Gemini, OpenAI, Qwen | **Anthropic Claude (default)**, OpenAI, Gemini, Ollama (disabled for T4) |
| **LLM response** | Plain text narration | **Structured JSON**: riskScore, explanation, suggestedAction, accountingRiskType, materialityAssessment, proposedCorrection |
| **LLM response tone** | General | Strict system prompt: story-first, business-owner language, no jargon, no instructions |
| **Settings** | Basic per-run options | **Full settings table** (`gl_settings`): per-detector enable flags, tier weights, thresholds, LLM API keys, scheduled runs, email/Slack alerts |
| **Feedback model** | Append-only (multiple rows) | **Upsert** per entry (one row, updated verdict); includes resolutionStatus + assignedTo |
| **Chat persistence** | Client-side state only | **Server-persisted** `gl_chat_messages` table |
| **Scheduled reports** | Not present | **Full PDF/XLSX/CSV report generator** with scheduling (daily/weekly/monthly) |
| **Notifications** | Email only | **Email (SMTP) + Slack webhook** |
| **Migration range** | 0057–0065 | **0062–0081+** |

---

## 3. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (React)                         │
│  GLReview (home) → GLDashboard → GLTransactionExplorer          │
│  GLAnomalyAnalysis → GLPeriodComparison → GLAIAssistant         │
│  GLSettings → GLReports                                          │
└────────────────────────┬────────────────────────────────────────┘
                         │ REST API (/api/gl-review/*)
┌────────────────────────▼────────────────────────────────────────┐
│                   Express API Server                             │
│  /upload  /runs  /entries  /analytics/*  /settings              │
│  /business-rules  /feedback  /export  /nlquery  /chat           │
└────────────────────────┬────────────────────────────────────────┘
                         │
           ┌─────────────┴─────────────────────┐
           │                                    │
┌──────────▼──────────────┐    ┌───────────────▼───────────────┐
│  glReviewWorker          │    │  Scheduler (60s poll)          │
│  (Bull Queue / in-proc)  │    │  - Creates auto runs           │
│                          │    │  - Generates scheduled reports  │
│  Phase 1: Load (0-5%)   │    │  - Sends email/Slack alerts    │
│  Phase 2: Profile (5-15%)│    └───────────────────────────────┘
│  Phase 3: Tier1 (15-35%) │
│  Phase 4: Tier2 (35-45%) │
│  Phase 5: Tier3 (45-50%) │
│  Phase 6: Pre-score      │
│          (50-75%)        │
│  Phase 7: Tier4 LLM      │
│          (75-90%)        │
│  Phase 8: Final score    │
│          (90-100%)       │
│  Phase 9: Bulk write     │
└──────────┬──────────────┘
           │
┌──────────▼─────────────────────────────────────────────────────┐
│                      PostgreSQL Database                         │
│  gl_runs  gl_entries  gl_anomalies  gl_settings                 │
│  gl_business_rules  gl_feedback  gl_chat_messages               │
│  gl_scheduled_reports                                            │
└────────────────────────────────────────────────────────────────┘
```

### Detection Model Overview

```
Every GL entry runs through all active tiers:

Tier 1 (32 detectors, always-on per enable flags):
  Statistical:  duplicate, near_duplicate, z_score_outlier, iqr_outlier,
                temporal_z_score, benford_law, last_digit_pattern,
                round_number, amount_clustering
  Temporal:     weekend_posting, holiday_posting, backdating, future_date,
                period_end_cluster, transaction_burst
  Behavioral:   split_transaction, reversal, threshold_breach,
                one_time_vendor, dormant_vendor, duplicate_invoice,
                excessive_journal_entries, rare_transaction_type
  Rule/Text:    suspicious_keyword, fraud_pattern, journal_balance,
                data_quality, description_mismatch, account_velocity,
                unusual_account_pairing, recurring_pattern, related_party

Tier 2 (7 detectors, opt-in):
  isolation_forest, dbscan, association_rule,
  copod, ecod, account_behavior, entity_behavior

Tier 3 (dynamic, from gl_business_rules):
  business_rule_breach

Tier 4 (LLM, only critical+high, max 100/run):
  llm_flagged

Score Fusion:
  Each tier's detectors contribute proportionally within their tier weight.
  rawScore = min(1, (Σ weighted scores) × 3.5)
  finalRiskScore = round(rawScore × 100)   → integer 0–100
  riskTier = thresholdLookup(finalRiskScore)
```

---

## 4. Database Schema

**File:** `shared/schema/tables-gl-review.ts`  
**Migrations:** `migrations/0062_gl_review_tables.sql` through `migrations/0081_*.sql`

### 4.1 `gl_runs`

One row per uploaded GL file / analysis job.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | Auto-increment job ID |
| `userId` | integer FK | Owner (auth user) |
| `status` | enum | `pending` \| `processing` \| `completed` \| `failed` \| `cancelled` |
| `fileName` | text | Original file name |
| `fileType` | text | `csv` \| `xlsx` \| `xls` |
| `fileSizeBytes` | integer | Upload size |
| `dateRangeFrom` | date | Earliest transaction date in file |
| `dateRangeTo` | date | Latest transaction date in file |
| `totalEntries` | integer | Total GL rows parsed |
| `flaggedCount` | integer | Rows with riskTier ≠ normal |
| `criticalCount` | integer | Rows at critical tier |
| `highCount` | integer | Rows at high tier |
| `mediumCount` | integer | Rows at medium tier |
| `lowCount` | integer | Rows at low tier |
| `normalCount` | integer | Rows at normal tier |
| `avgRiskScore` | numeric | Average composite risk score |
| `progressPercentage` | integer | Live progress 0–100 |
| `startedAt` | timestamp | Worker start time |
| `completedAt` | timestamp | Worker finish time |
| `errorMessage` | text | Failure reason (if status=failed) |
| `dataProfile` | JSONB | Statistical summary (min/max/avg amounts, date range, account counts, etc.) |
| `materialExposure` | numeric | Sum of amounts for critical+high entries |
| `sourceFormat` | text | `quickbooks` \| `generic` |
| `queueJobId` | text | Bull job ID for status tracking |

### 4.2 `gl_entries`

Canonical GL row store. One row per journal line item.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `runId` | integer FK → gl_runs(id) | |
| `userId` | integer FK | Denormalized for fast per-user queries |
| `rowIndex` | integer | Original row position in file |
| `date` | date | Transaction / value date |
| `journalRef` | text | Journal entry document reference (e.g., "GJ-001") |
| `txnType` | text | Transaction type (extracted from journalRef prefix) |
| `account` | text | Full account name |
| `accountType` | text | Asset \| Liability \| Equity \| Revenue \| Expense |
| `debit` | numeric | Debit amount (null if credit line) |
| `credit` | numeric | Credit amount (null if debit line) |
| `balance` | numeric | Running balance (from source if available) |
| `description` | text | Narration / memo |
| `party` | text | Vendor / customer / entity name |
| `postedBy` | text | System user who posted the entry |
| `riskScore` | integer | Composite risk score **0–100** |
| `riskTier` | enum | `critical` \| `high` \| `medium` \| `low` \| `normal` |
| `anomalyFlags` | text[] | Array of fired anomaly type codes |
| `aiExplanation` | text | LLM-generated explanation (plain English, ≤500 chars) |
| `riskExplanation` | JSONB | Structured LLM response: suggestedAction, accountingRiskType, materialityAssessment, proposedCorrection |
| `status` | enum | `pending` \| `reviewed` \| `flagged` |

**Indexes:** `(runId)`, `(userId)`, `(runId, riskTier)`, `(runId, status)`

### 4.3 `gl_anomalies`

One row per (entry, anomalyType) pair. Stores per-detector output.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `entryId` | integer FK → gl_entries(id) | |
| `runId` | integer FK | Denormalized |
| `userId` | integer FK | Denormalized |
| `anomalyType` | text | Detector code string (see full list in §6) |
| `detectorScore` | numeric | Per-detector signal score 0.0–1.0 |
| `confidenceScore` | numeric | Optional secondary confidence metric |
| `riskReasons` | text[] | Human-readable reasons from this detector |
| `metadata` | JSONB | Detector-specific evidence (e.g., z-score value, matched keyword, peer amounts, Isolation Forest top drivers) |

**Indexes:** `(entryId)`, `(runId)`, `(runId, anomalyType)`

### 4.4 `gl_settings`

Per-user configuration. One row per user (unique constraint on userId).

| Column | Type | Default | Notes |
|---|---|---|---|
| `userId` | integer FK (unique) | — | |
| `thresholdCritical` | integer | 65 | Score ≥ this = Critical |
| `thresholdHigh` | integer | 40 | Score ≥ this = High |
| `thresholdMedium` | integer | 20 | Score ≥ this = Medium |
| `thresholdLow` | integer | 8 | Score ≥ this = Low |
| `weightTier1Statistical` | numeric | 0.25 | Tier-1 weight |
| `weightTier2Ml` | numeric | 0.35 | Tier-2 weight |
| `weightTier3Rules` | numeric | 0.25 | Tier-3 weight |
| `weightTier4Llm` | numeric | 0.15 | Tier-4 weight |
| `llmProvider` | text | `anthropic` | `anthropic` \| `openai` \| `gemini` \| `ollama` \| `disabled` |
| `llmModel` | text | `claude-sonnet-4-6` | Model string |
| `apiKeyAnthropic` | text | — | Encrypted / env-resolved |
| `apiKeyOpenai` | text | — | |
| `apiKeyGoogle` | text | — | |
| `enableBenfordLaw` | boolean | true | Toggle Tier-1 detector |
| `enableRoundNumber` | boolean | true | Toggle Tier-1 detector |
| `enableThresholdBreach` | boolean | true | Toggle Tier-1 detector |
| `enableBackdating` | boolean | true | Toggle Tier-1 detector |
| `enablePeriodEndCluster` | boolean | true | Toggle Tier-1 detector |
| `enableFraudPatterns` | boolean | true | Toggle Tier-1 detector |
| `enableNearDuplicates` | boolean | true | Toggle Tier-1 detector |
| `enableIsolationForest` | boolean | true | Toggle Tier-2 detector |
| `enableDbscan` | boolean | true | Toggle Tier-2 detector |
| `enableAssociationRule` | boolean | true | Toggle Tier-2 detector |
| `enableCopod` | boolean | true | Toggle Tier-2 detector |
| `enableEcod` | boolean | true | Toggle Tier-2 detector |
| `enableBehaviorProfiling` | boolean | true | Toggle account+entity behavior detectors |
| `scheduledEnabled` | boolean | false | Auto-run on schedule |
| `scheduledFrequency` | text | — | `daily` \| `weekly` \| `monthly` |
| `scheduledDayOfWeek` | integer | — | 0=Sun, 6=Sat (weekly only) |
| `scheduledHour` | integer | — | Hour (0–23) to trigger run |
| `emailAlerts` | boolean | false | Send email on run completion |
| `alertEmail` | text | — | Single or comma-separated addresses |
| `slackWebhook` | text | — | Slack incoming webhook URL |
| `fiscalYearStartMonth` | integer | 1 | Affects quarter-end detection |
| `relatedPartyList` | JSONB | [] | `[{name, relationship}]` — fed to related_party detector |

### 4.5 `gl_business_rules`

User-defined custom rules applied as Tier-3 detectors.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `userId` | integer FK | |
| `ruleName` | text | Human label |
| `description` | text | Description of the rule's intent |
| `condition` | JSONB | Rule condition (see §6.3 for schema) |
| `severity` | enum | `critical` \| `high` \| `medium` \| `low` |
| `isActive` | boolean | Whether applied in new runs |

### 4.6 `gl_feedback`

Auditor verdicts — **upsert model** (one row per entry, updated in place).

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `entryId` | integer FK (unique) | One feedback record per entry |
| `runId` | integer FK | |
| `userId` | integer FK | Last auditor to update |
| `status` | enum | `pending` \| `confirmed_true_positive` \| `dismissed_false_positive` |
| `resolutionStatus` | enum | `unresolved` \| `resolved` \| `escalated` |
| `auditDecision` | text | Free-form decision text |
| `comments` | text | Auditor notes |
| `requiredEvidence` | text | Evidence requested/obtained |
| `resolutionNotes` | text | How the issue was resolved |
| `reviewedBy` | text | Auditor name/identifier |
| `reviewedAt` | timestamp | When reviewed |
| `assignedTo` | text | Assigned reviewer |

**Design note:** Unlike the append-only pattern in earlier implementations, this uses upsert — the current verdict replaces the prior one, but `reviewedAt` provides the audit timestamp.

### 4.7 `gl_chat_messages`

Server-persisted multi-turn chat history per run.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `runId` | integer FK | |
| `userId` | integer FK | |
| `role` | enum | `user` \| `assistant` \| `system` |
| `content` | text | Message text |
| `metadata` | JSONB | Optional: model used, token counts, etc. |

### 4.8 `gl_scheduled_reports`

Report generation configuration.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `userId` | integer FK | |
| `reportName` | text | Label |
| `reportType` | text | `executive_risk_summary` \| `transaction_detail` \| `anomaly_detection` \| `audit_trail` \| `period_comparison` \| `vendor_risk` |
| `format` | text | `pdf` \| `xlsx` \| `csv` |
| `frequency` | text | `daily` \| `weekly` \| `monthly` |
| `recipients` | text[] | Email addresses |
| `status` | text | `active` \| `paused` |

---

## 5. File Parsing & Normalization

**File:** `server/services/glReview/glFileParserService.ts`

### 5.1 Supported Formats

| Format | Details |
|---|---|
| CSV | Comma, tab, semicolon delimiters — auto-detected |
| XLSX | First sheet read via `xlsx` library |
| XLS | Legacy Excel format, same handling |

Max file size: **50 MB** (Multer memory storage)

### 5.2 Column Auto-Detection

The parser maps common ERP column name variations to canonical fields:

| Canonical Field | Detected Aliases |
|---|---|
| `date` | `Date`, `PostDate`, `date`, `DATE`, `Transaction Date`, `Txn Date` |
| `journalRef` | `Journal Ref`, `Reference`, `JournalRef`, `Ref`, `Document Number` |
| `account` | `Account`, `Account Name`, `GL Account`, `AccountName` |
| `accountType` | `Account Type`, `Type`, `AccountType` |
| `debit` | `Debit`, `DR`, `Debit Amount` |
| `credit` | `Credit`, `CR`, `Credit Amount` |
| `balance` | `Balance`, `Running Balance`, `Net` |
| `description` | `Description`, `Memo`, `Narration`, `Remarks`, `Details` |
| `party` | `Party`, `Vendor`, `Customer`, `Supplier`, `Counterparty`, `Name` |
| `postedBy` | `Posted By`, `User`, `Entered By`, `Preparer` |

**Response includes:** `detectedColumns: Array<{ name: string; mappedField: string }>` for UI preview confirmation.

### 5.3 Source Format Detection

- `quickbooks`: Detected from header structure or file metadata
- `generic`: All other ERP exports

### 5.4 Data Extraction

- `txnType`: Extracted from `journalRef` prefix (e.g., `"GJ-001"` → `"GJ"`)
- `accountType`: Inferred from account name via keyword matching (20 categories: Bank/Checking, Accounts Receivable, Inventory, Fixed Assets, Revenue, COGS, etc.)
- `accountSubtype`: Further refinement of accountType (used in LLM context)
- `materialityThreshold`: `max(1000, totalDebits × 0.005)` — 0.5% of run's total debits

---

## 6. Four-Tier Detector Framework

### 6.1 Tier-1 — Statistical & Rule-Based (32 Detectors)

**Always-on** (subject to per-detector enable flags in `gl_settings`).  
All Tier-1 detectors are synchronous, CPU-bound, and run in two groups separated by an event-loop yield to avoid blocking.

**Group A (fast, O(n) / O(n log n)):**

#### `duplicate`
Exact duplicate detection. Key: `(account, debit, credit, description, date)`. Any group >1 row → all members flagged. Score: 1.0.

#### `near_duplicate`
Similar transactions posted close together (within configurable time window). Triggered by `enableNearDuplicates`. Fuzzy match on amount + description similarity. Score: 0.7–0.95.

#### `z_score_outlier`
Per-account Z-Score on `debit` and `credit` amounts. Requires ≥5 entries in account group (falls back to global). Flags if `|z| > 3.0`. Score: scales with z magnitude.

#### `iqr_outlier`
Interquartile Range outlier on amounts. Flags entries below `Q1 - 1.5×IQR` or above `Q3 + 1.5×IQR`. Complements z-score for non-normal distributions.

#### `temporal_z_score`
Time-series Z-Score on posting frequency per account. Flags burst periods where posting count exceeds `mean + 2σ` for that account's monthly pattern.

#### `benford_law`
First-digit frequency analysis. Triggered by `enableBenfordLaw`. Minimum 10 entries per account. Expected: `log₁₀(1 + 1/d)`. Flags entries where their first digit is overrepresented by >10%.

#### `last_digit_pattern`
Detects unnatural last-digit distribution (e.g., too many `.00` endings, too many `.99` endings). Complementary to round_number.

#### `round_number`
Exact multiples of 100 (no cents). Triggered by `enableRoundNumber`. Score: 1.0 for multiples of $1K+, 0.7 for multiples of $100.

#### `amount_clustering`
Detects suspicious concentration of amounts around a specific value (e.g., many entries clustered between $9,800–$9,999 below a $10K threshold). Score proportional to cluster density.

#### `weekend_posting`
`date.getDay()` ∈ {0, 6}. Score: 1.0.

#### `holiday_posting`
Transaction date matches public holidays (locale-aware via date library). Score: 0.9.

#### `backdating`
Triggered by `enableBackdating`. Compares transaction `date` vs `postedAt` system timestamp (or `createdDate` from file). Flags if lag > 1 day. Score scales with lag days, caps at 1.0 for 30+ days.

#### `future_date`
`date > runDate`. Flags entries with future-dated transactions. Score: 1.0.

#### `period_end_cluster`
Triggered by `enablePeriodEndCluster`. Uses `fiscalYearStartMonth` to compute month-end, quarter-end, and year-end boundaries. Flags when unusual concentration of entries (by count or amount) falls in last N days of a period. Score based on deviation from typical period-end ratio.

#### `transaction_burst`
Velocity spike: flags entries in a short time window where entry count exceeds `μ + 3σ` of typical daily posting frequency.

**Group B (heavier, requires grouping):**

#### `split_transaction`
Detects entries that appear to artificially split a single larger transaction across multiple lines or dates (structuring). Looks for entries with the same party/account and amounts that sum close to a threshold.

#### `reversal`
Unusual reversal patterns: a reversal posted far (>30 days) after the original entry, or a reversal with no matching original. Score proportional to gap or orphan status.

#### `threshold_breach`
Triggered by `enableThresholdBreach`. Configurable per-user thresholds per account or global. Flags entries exceeding defined approval limits. Score: 1.0.

#### `one_time_vendor`
First and only occurrence of this `party` in the entire run. Score: 0.7. (Higher risk if amount is also large relative to materiality threshold.)

#### `dormant_vendor`
Vendor reactivated after long period of inactivity (inferred from no prior entries in same run's date range). Score: 0.75.

#### `duplicate_invoice`
Same `journalRef` or description pattern billed multiple times across different dates. Distinct from `duplicate` (which requires exact same date too).

#### `excessive_journal_entries`
Unusually high number of line items under one `journalRef`. Flags journal entries with >N lines (N inferred from distribution of journal sizes in the run).

#### `rare_transaction_type`
`txnType` appears in <1% of entries for this account. Flags uncommon combinations like a Credit Note to a Revenue account.

#### `suspicious_keyword`
Description contains keywords: `"cash"`, `"adjustment"`, `"void"`, `"write-off"`, `"correction"`, `"miscellaneous"`, `"suspense"`, and others. Score varies by keyword severity.

#### `fraud_pattern`
Triggered by `enableFraudPatterns`. Matches known fraud scheme patterns: ghost vendor + round amount, account truncation structuring, lapping-like sequences. Score: 0.9–1.0.

#### `journal_balance`
Groups entries by `journalRef`. Checks `Σdebits = Σcredits` for each journal. Flags journals where `|debits - credits| > $0.01`. Score: proportional to imbalance amount.

#### `data_quality`
Flags data quality issues: null required fields, invalid dates, negative debit/credit values, non-numeric amounts. Score: 0.6 for minor issues, 0.9 for critical missing data.

#### `description_mismatch`
Account type doesn't match description context (e.g., "salary" in a Revenue account, "revenue" in an Expense account). Score: 0.65.

#### `account_velocity`
Unusual posting frequency for a specific account relative to its historical pattern within the run. Score scales with deviation.

#### `unusual_account_pairing`
Unexpected debit/credit account combinations across the same journal (e.g., Revenue debited against Liability credited instead of standard pairing). Score: 0.7.

#### `recurring_pattern`
Suspiciously regular repeated amounts and descriptions across different dates (e.g., same $12,500 to same vendor every 15 days). Score: 0.75 (could indicate unauthorized recurring payment).

#### `related_party`
`party` name fuzzy-matches an entry in `relatedPartyList` (from `gl_settings`). Score: 0.8 + severity modifier based on relationship type.

---

### 6.2 Tier-2 — Machine Learning (7 Detectors)

**Opt-in** via per-detector enable flags in `gl_settings`. All Tier-2 detectors are multivariate and O(n²) or worse in naive implementation — triggered with configurable sample cap.

**Sample limit:** If `totalEntries > 3000`, Tier-2 detectors sample down to `ML_SAMPLE_LIMIT = 3000` rows to bound computation time.

All Tier-2 detectors run in `Promise.all()` (parallel) with individual try/catch (one failure does not abort others).

---

#### `isolation_forest`
**File:** `server/services/glReview/isolationForestDetectionService.ts`

**Hyperparameters:**
- `NUM_TREES = 100`
- `SUBSAMPLE = 256`
- `MIN_ENTRIES = 10` (skip run if fewer entries)
- `SCORE_CUTOFF = 0.55` (anomalyScore > cutoff → flagged)

**Feature vector (9 features, all normalized to [0,1]):**
| Feature | Computation |
|---|---|
| `log_amount` | `log₁₀(amount + 1)` / max_log_amount |
| `day_of_week` | `dayOfWeek / 6` |
| `day_of_month` | `(day - 1) / 30` |
| `account_type` | Encoded: Asset=0.1, Liability=0.2, Equity=0.3, Revenue=0.4, Expense=0.5, Other=0.6 |
| `amount_to_account_avg` | `min(10, amount / accountAvg) / 10` |
| `is_month_end` | `1` if `day ≥ 28`, else `0` |
| `is_quarter_end` | `1` if quarter-end and `day ≥ 28`, else `0` |
| `is_round_amount` | `1` if `amount % 100 === 0`, else `0` |
| `description_length` | `min(wordCount, 20) / 20` |

**Output:** Anomaly score (Isolation Forest path-length formula), top 3 driver features (z-score attribution across features), included in `metadata` JSONB of `gl_anomalies`.

---

#### `dbscan`
**File:** `server/services/glReview/dbscanDetectionService.ts`

Density-based clustering. Noise points (not assigned to any cluster) are flagged as anomalies. Feature space: normalized `(amount, day_of_month, account_type_encoded)`.

---

#### `association_rule`
**File:** `server/services/glReview/associationRuleDetectionService.ts`

**Pattern:** `(debitAccount, creditAccount, transactionType, amountBucket, isMonthEnd)`

**Amount bucketing:** Per-account median as baseline; categories: `micro`, `small`, `normal`, `large`, `extreme`.

**Scoring:**
- `RARE_THRESHOLD = 5` — pattern appears <5 times in run
- `RARE_SCORE = 0.55`
- `SINGLETON_SCORE = 0.80` — pattern appears exactly once
- If account pair is common but full 5-tuple is rare: `score × 0.75`

---

#### `copod` (Co-Occurrence Pattern Outlier Detection)
**File:** `server/services/glReview/copodDetectionService.ts`

COPOD algorithm: computes empirical copula-based outlier scores. Distribution-free, handles skewed accounting data without assuming normality.

---

#### `ecod` (Ensemble-based Copula Outlier Detection)
**File:** `server/services/glReview/ecodDetectionService.ts`

Ensemble variant of COPOD. Combines multiple copula estimations for more robust detection on small datasets.

---

#### `account_behavior`
**File:** `server/services/glReview/accountBehaviorDetectionService.ts`

Builds a behavioral profile per account from the run's data:
- Typical amount range (mean ± 2σ)
- Typical posting days
- Typical transaction types
- Typical party/vendor set

Flags entries that deviate significantly from the account's established profile within this run.

---

#### `entity_behavior`
**File:** `server/services/glReview/entityBehaviorDetectionService.ts`

Same profiling approach but at the vendor/entity level:
- Vendor's typical amount range
- Vendor's typical accounts
- Vendor's typical posting frequency

Flags entries where the vendor behaves differently from their own pattern.

---

### 6.3 Tier-3 — Business Rules Engine

**File:** `server/services/glReview/businessRulesService.ts`

User-defined rules are loaded from `gl_business_rules` at job start. Applied to every entry as the third scoring tier.

#### Condition Type 1: Field-Based

```typescript
{
  field: "debit" | "credit" | "balance" | "date" | "account" |
         "accountType" | "journalRef" | "description" | "party" |
         "riskScore" | "status";
  operator: "gt" | "gte" | "lt" | "lte" | "eq" |
             "contains" | "not_contains" | "is_null";
  value: number | string | boolean;
}
```

#### Condition Type 2: Account Threshold

```typescript
{
  type: "account_threshold";
  account: string;        // Exact account name match
  direction: "debit" | "credit" | "any";
  amount: number;         // Threshold value
  // Flags: entry.account === account AND entry[direction] > amount
}
```

#### Severity → Score Mapping

| Rule Severity | Tier-3 Signal Score |
|---|---|
| `critical` | 1.00 |
| `high` | 0.75 |
| `medium` | 0.50 |
| `low` | 0.30 |

**Behavior:**
- If an entry matches multiple rules, the **highest** score wins
- Metadata includes: `matchedRuleId`, `matchedRuleName`, `severity`
- Rules are evaluated against denormalized entry fields (no DB join at eval time)

---

### 6.4 Tier-4 — LLM Narrative Scoring

**File:** `server/services/glReview/llmNarrativeService.ts`

AI contextual scoring and explanation generation. Only runs after pre-scoring (Phase 6) determines which entries are critical or high.

#### Eligibility Criteria

- Entry pre-score tier must be `critical` or `high` (`LLM_FLAG_TIERS`)
- Maximum **100 entries** per run (`LLM_MAX_ENTRIES`)
- Entries matching GST/tax keywords are **excluded**:
  - Account/description/party contains: `"gst"`, `"hst"`, `"vat"`, `"pst"`, `"sales tax"`, `"tax payable"`, `"tax remittance"`
  - Known tax agency names (ATO, CRA, HMRC, IRS, etc.)

#### Supported Providers

| Provider | Default Model | Notes |
|---|---|---|
| `anthropic` | `claude-sonnet-4-6` | Default provider |
| `openai` | Configurable | Any OpenAI chat model |
| `gemini` | Configurable | Google Gemini |
| `ollama` | — | Disabled for Tier-4 (local models not suitable for production scoring) |
| `disabled` | — | No LLM tier |

#### Input Context per Entry (sent to LLM)

```json
{
  "id": "entry row ID",
  "description": "...",
  "debit": 12500.00,
  "credit": null,
  "totalAmount": 12500.00,
  "account": "Office Supplies Expense",
  "accountType": "Expense",
  "accountSubtype": "Operating Expense",
  "party": "ABC Stationery Ltd",
  "date": "2024-03-29",
  "txnType": "GJ",
  "isMonthEnd": true,
  "isWeekend": false,
  "journalRef": "GJ-0042",
  "anomalyFlags": ["period_end_cluster", "round_number", "z_score_outlier"],
  "riskScore": 71,
  "journalContext": [/* other lines in same GJ-0042 */],
  "materialityThreshold": 8500
}
```

#### System Prompt Constraints (Strictly Enforced)

The LLM system prompt mandates:
1. **Story-first, plain English** for business owners — no data science jargon
2. **Prohibited terms:** z-score, anomaly, algorithm, threshold, model, flagged by, outlier, statistical, deviation
3. **Prohibited instructions:** "you should", "investigate", "verify", "escalate", "check", "review"
4. **Prohibited openings:** "This transaction", "This entry"
5. **No GST/tax descriptions** as unusual (tax entries are filtered before reaching LLM)
6. **Length:** ≤500 characters for `explanation`, ≤120 characters for `actionDetail`

#### LLM Response Schema (Zod-validated)

```typescript
{
  id: string;
  riskScore: number;              // 0–100 LLM-adjusted score
  explanation: string;            // ≤500 chars, plain English story
  suggestedAction:
    | "investigate"
    | "verify_documentation"
    | "approve"
    | "escalate_to_cfo"
    | "mark_normal";
  actionDetail: string;           // ≤120 chars, one sentence
  flags: string[];                // Detector codes (unchanged from input)
  contextualRiskBoost: number;    // 0.0–0.2 extra weight from LLM
  accountingRiskType:
    | "fraud_indicator"
    | "duplicate_payment"
    | "misclassification"
    | "cutoff_error"
    | "unauthorized_transaction"
    | "data_entry_error"
    | "policy_violation"
    | "unusual_timing"
    | "normal";
  materialityAssessment: "material" | "borderline" | "immaterial";
  missingReversal: boolean;       // LLM detects a reversal should exist
  proposedCorrection?: {
    suggestedAccount: string;
    reason: string;
    memo: string;
    lines: Array<{ account, debit, credit, description }>;
  };
}
```

#### Batching & Concurrency

| Parameter | Value |
|---|---|
| `BATCH_SIZE` | 5 entries per LLM API call |
| `LLM_CONCURRENCY` | 4 concurrent batch promises |
| `TIMEOUT_MS` | 60 seconds per batch |
| `RETRY_DELAY_MS` | 3 seconds (one retry on timeout) |

**Error handling:** If LLM fails for a batch, entries get `aiStatus: "failed"` and retain their pre-score. JSON parsing handles markdown code block stripping and normalizes varied response shapes (`results`, `data`, `entries`, `analyses`, `transactions` keys).

**Legacy threshold detection:** On settings load, if old thresholds are detected (critical=80|75, high=60|50, medium=40|30, low=20|15), they are automatically replaced with new defaults, because old thresholds would produce no Critical/High under the weighted-average model.

---

## 7. Risk Score Fusion & Tier Classification

**File:** `server/services/glReview/riskScoringService.ts`

### Per-Tier Score Aggregation

For each tier `T` with weight `wT`:
1. Get the set of detectors that fired for this entry within tier `T`
2. Each detector contributes its signal score `sᵢ` (0–1)
3. Tier contribution = `wT × (Σ sᵢ / N_active_in_T)` where `N_active_in_T` = count of active detectors in tier

### Weight Redistribution

If a tier has **zero** active detectors, its weight `wT` is distributed proportionally to the other tiers with active detectors. This ensures weights always sum to 1.0 regardless of which detectors are enabled.

### Score Amplification & Final Score

```
totalNumerator = Σ (tier contributions across all tiers)
rawScore       = min(1.0, totalNumerator × SCORE_AMPLIFIER)   // SCORE_AMPLIFIER = 3.5
finalRiskScore = round(rawScore × 100)                         // Integer 0–100
```

**Why 3.5 amplifier?** Calibration constant: ensures that a single strong Tier-1 signal (score ~0.9, weight 0.25) produces a final score around 79, landing in "high" tier, rather than 22 (medium). Without amplification, the weighted-average would rarely exceed 30.

### Risk Tier Lookup (Configurable)

| Tier | Default Threshold | Score Range |
|---|---|---|
| `critical` | ≥ 65 | 65–100 |
| `high` | ≥ 40 | 40–64 |
| `medium` | ≥ 20 | 20–39 |
| `low` | ≥ 8 | 8–19 |
| `normal` | < 8 | 0–7 |

Thresholds are read from `gl_settings.thresholdCritical/High/Medium/Low` per user.

---

## 8. Worker Execution Pipeline

**File:** `server/workers/glReviewWorker.ts`

**Queue:** `glReviewQueue` (Bull with Redis; falls back to in-process if Redis unavailable)  
**Job timeout:** `JOB_TIMEOUT_MS = 600000` (10 minutes)  
**Queue de-duplication:** Avoids duplicate runs within 2 hours for scheduled triggers

### Execution Phases

| Phase | Progress % | Description |
|---|---|---|
| **Phase 1: Load** | 0–5% | Load `gl_entries` for runId from DB |
| **Phase 2: Data Profile** | 5–15% | Compute statistical summary: min/max/avg amounts, date range, unique accounts, unique parties, debit/credit totals, `materialityThreshold` → write to `gl_runs.dataProfile` |
| **Phase 3: Tier-1** | 15–35% | Run 32 statistical detectors in 2 groups (A then yield then B); each detector emits `Map<entryId, {score, metadata}>` |
| **Phase 4: Tier-2** | 35–45% | `Promise.all()` over 7 ML detectors; sample to 3000 if needed; each with try/catch |
| **Phase 5: Tier-3** | 45–50% | Load active `gl_business_rules`; evaluate each rule against each entry |
| **Phase 6: Pre-score** | 50–75% | Compute composite score WITHOUT Tier-4; derive `riskTier`; select `critical` + `high` entries for LLM (up to 100); exclude GST/tax entries |
| **Phase 7: Tier-4 LLM** | 75–90% | Batch 5 entries per call; 4 concurrent batches; 60s timeout; structured JSON response; `contextualRiskBoost` merged back |
| **Phase 8: Final score** | 90–100% | Recompute composite score for ALL entries (now including Tier-4 signals); derive final `riskTier` |
| **Phase 9: Bulk write** | 100% | Update all `gl_entries` rows: `riskScore`, `riskTier`, `anomalyFlags`, `aiExplanation`, `riskExplanation`; write `gl_anomalies` rows; update `gl_runs` counts and `status=completed` |

**Progress updates:** `progressPercentage` on `gl_runs` is updated at each phase transition, enabling real-time frontend polling.

**GST/tax filter (Phase 6):** Entries matching any of these patterns are excluded from Tier-4:
- Account, description, or party contains: `gst`, `hst`, `vat`, `pst`, `sales tax`, `tax payable`, `tax remittance`
- Known tax agency names: ATO, CRA, HMRC, IRS, CBDT, SARS, BIR, and others

**Stuck detection:** Frontend polls for runs in `processing` status >30 minutes and surfaces a "stuck" warning with a Retry option.

---

## 9. Settings & Configuration System

**File:** `server/services/glReview/glSettingsService.ts`  
**Storage:** `gl_settings` table (one row per user, upserted)

### Defaults Applied on First Access

```typescript
const DEFAULT_SETTINGS = {
  thresholdCritical: 65,
  thresholdHigh: 40,
  thresholdMedium: 20,
  thresholdLow: 8,
  weightTier1Statistical: 0.25,
  weightTier2Ml: 0.35,
  weightTier3Rules: 0.25,
  weightTier4Llm: 0.15,
  llmProvider: 'anthropic',
  llmModel: 'claude-sonnet-4-6',
  enableBenfordLaw: true,
  enableRoundNumber: true,
  enableThresholdBreach: true,
  enableBackdating: true,
  enablePeriodEndCluster: true,
  enableFraudPatterns: true,
  enableNearDuplicates: true,
  enableIsolationForest: true,
  enableDbscan: true,
  enableAssociationRule: true,
  enableCopod: true,
  enableEcod: true,
  enableBehaviorProfiling: true,
  fiscalYearStartMonth: 1,
  relatedPartyList: [],
  emailAlerts: false,
  scheduledEnabled: false,
};
```

### API Key Testing

`POST /api/gl-review/test-api-key` — Sends a minimal test prompt to the configured LLM provider to validate the API key before a real run. Returns `{ success: boolean, error?: string }`.

### Scheduled Run Trigger Logic (Scheduler, 60s poll)

```
Every 60 seconds:
  For each user with scheduledEnabled=true:
    Determine if run should trigger based on frequency/dayOfWeek/hour
    Check: last completed run was > 2 hours ago
    If eligible:
      Fetch most recent GL file for user (from last manual run)
      Create new run + enqueue job
```

---

## 10. Backend API Routes

**Mount:** `/api/gl-review/` (all routes require authentication middleware)

### Upload & Run Management

| Method | Path | Description |
|---|---|---|
| `POST` | `/upload` | Multipart file upload; returns `{ success, runId, totalEntries, sourceFormat, detectedColumns }` |
| `GET` | `/runs` | List all runs for authenticated user (ordered by createdAt desc) |
| `GET` | `/runs/:id` | Single run details (includes `dataProfile`, counts, `progressPercentage`) |
| `GET` | `/runs/:id/summary` | Extended summary with anomaly type breakdown and top entries |
| `POST` | `/runs/:id/retry` | Re-queue a failed run (resets status to `pending`) |
| `DELETE` | `/runs/:id` | Delete run and all child data (cascade) |

### Entry Management & Feedback

| Method | Path | Description |
|---|---|---|
| `GET` | `/runs/:id/entries` | Paginated entries with filters (see query params below) |
| `GET` | `/runs/:id/entries/all` | All entries without pagination (for export) |
| `PATCH` | `/runs/:id/entries/bulk` | Bulk update `status` for multiple entry IDs |
| `PATCH` | `/entries/:entryId/status` | Update single entry status |
| `PUT` | `/runs/:id/entries/:entryId/feedback` | Upsert auditor verdict + comments + resolution status |

**`GET /entries` query parameters:**

| Param | Type | Description |
|---|---|---|
| `riskTier` | string | Filter: `critical/high/medium/low/normal` |
| `status` | string | Filter: `pending/reviewed/flagged` |
| `account` | string | Exact account name match |
| `search` | string | Text search across description, party, journalRef |
| `dateFrom` | ISO date | Transaction date range start |
| `dateTo` | ISO date | Transaction date range end |
| `anomalyType` | string | Filter by specific anomaly flag code |
| `sortBy` | string | `riskScore` \| `date` \| `amount` \| `rowIndex` |
| `sortDir` | string | `asc` \| `desc` |
| `page` | number | Page number (default: 1) |
| `pageSize` | number | Rows per page (default: 50, max: 200) |

**Response includes per entry:** full entry fields + `anomalyDetails` (per-entry `gl_anomalies` rows flattened) + `accountStats` (account-level aggregate stats for context).

### Analytics

| Method | Path | Description |
|---|---|---|
| `GET` | `/runs/:id/analytics/period-metrics` | Monthly breakdown: `{ month, totalAmount, flaggedAmount, flaggedCount, avgRiskScore }[]` |
| `GET` | `/runs/:id/analytics/entity-risk` | Vendor risk ranking: `{ party, maxRiskScore, flaggedCount, totalAmount }[]` |
| `GET` | `/runs/:id/analytics/anomaly-breakdown` | Count + % per anomaly type code |

### Settings & Business Rules

| Method | Path | Description |
|---|---|---|
| `GET` | `/settings` | Get user's `gl_settings` row (creates defaults if none) |
| `PUT` | `/settings` | Update settings (partial; unset fields retain existing values) |
| `POST` | `/test-api-key` | Test LLM API key validity |
| `GET` | `/business-rules` | List all rules for user |
| `POST` | `/business-rules` | Create new rule |
| `PUT` | `/business-rules/:id` | Update rule (condition, severity, isActive) |
| `DELETE` | `/business-rules/:id` | Delete rule |

### Other

| Method | Path | Description |
|---|---|---|
| `POST` | `/export` | CSV export of filtered entries |
| `GET` | `/scheduled-reports` | List scheduled report configs |
| `POST` | `/scheduled-reports` | Create scheduled report config |
| `POST` | `/nlquery` | Natural language query over run data (LLM-powered) |

---

## 11. Analytics & Reporting

### 11.1 Analytics Endpoints

**Period Metrics** (`/analytics/period-metrics`):
- Groups entries by calendar month
- Computes: `totalAmount` (sum of debit+credit), `flaggedAmount`, `flaggedCount`, `avgRiskScore`
- Used by **GLPeriodComparison** page for trend charts

**Entity Risk** (`/analytics/entity-risk`):
- Groups by `party`
- Returns: `maxRiskScore`, `flaggedCount`, `totalAmount`, `mostCommonFlags`
- Used by **GLAnomalyAnalysis** entity risk ranking table

**Anomaly Breakdown** (`/analytics/anomaly-breakdown`):
- Counts occurrences of each anomaly type across all entries in run
- Returns `{ anomalyType, count, percentage }[]`
- Used by **GLAnomalyAnalysis** breakdown chart

### 11.2 Scheduled Report Types

**File:** `server/services/glReview/reportGenerationService.ts`

| Report Type | Format | Content |
|---|---|---|
| `executive_risk_summary` | PDF | Overview KPIs, risk distribution chart, key findings narrative |
| `transaction_detail` | XLSX | All entries with anomaly flags, risk scores, AI explanations |
| `anomaly_detection` | PDF | Breakdown by anomaly type, trend charts, top flagged entries |
| `audit_trail` | CSV | All entries + review decisions + feedback + timestamps |
| `period_comparison` | PDF | Month-over-month or year-over-year risk trends |
| `vendor_risk` | XLSX | Vendor risk ranking, payment history, anomaly summary |

Reports are generated on schedule and emailed to configured recipients. They can also be triggered manually from **GLReports** page.

---

## 12. Notification System (Email + Slack)

### 12.1 Email Notifications

**File:** `server/services/glReview/emailService.ts`

**Transport:** SMTP configured from environment variables:
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS`, `SMTP_FROM`
- Falls back to console logging if SMTP not configured

**Triggers:**
- Run completion (if `emailAlerts=true` in settings)
- Scheduled report delivery
- Stuck run detection alert

**Email content:** Run summary (fileName, totalEntries, flaggedCount, criticalCount, highCount, avgRiskScore, link to dashboard)

### 12.2 Slack Notifications

**File:** `server/services/glReview/slackAlertService.ts`

**Transport:** HTTP POST to `slackWebhook` URL (Slack Incoming Webhooks)

**Message format:**
```
GL Review Complete: [fileName]
📊 [totalEntries] entries analyzed
🔴 [criticalCount] Critical | 🟠 [highCount] High
📈 Avg Risk Score: [avgRiskScore]
🔗 View Results: [dashboardLink]
```

**Trigger:** Run completion (if `slackWebhook` is set in `gl_settings`)

---

## 13. Frontend Architecture

### 13.1 Pages

**Directory:** `src/pages/glReview/`

#### `GLReview.tsx` — Home / Run List
**Route:** `/gl-review`

Features:
- Upload zone (drag-and-drop, `.csv`, `.xlsx`, `.xls`)
- Live upload progress indicator
- Runs list table: fileName, status badge, date range, totalEntries, flaggedCount, criticalCount, avgRiskScore, started/completed timestamps
- Actions: View Dashboard, Delete Run
- **Stuck detection:** Runs in `processing` status for >30 minutes show warning + Retry button
- Polling: Refreshes list every 5s while any run is `pending` or `processing`

#### `glReview/GLDashboard.tsx` — Executive Dashboard
**Route:** `/gl-review/:runId/dashboard`

Features:
- Run status banner with live `progressPercentage` bar (while processing)
- **KPI cards:** Total Entries, Flagged, Critical+High, Material Exposure ($)
- **Risk distribution** donut chart (Recharts `PieChart` — 5 tiers)
- **Score distribution** histogram (Recharts `BarChart` — decile buckets)
- **Top flagged accounts** bar chart
- **AI Executive Summary** section (plain text, from `gl_runs.aiExecutiveSummary` — note: different from the structured `riskExplanation` per entry)
- **Recent runs** sidebar for quick navigation
- Auto-refresh every 3s while run is processing

#### `glReview/GLTransactionExplorer.tsx` — Transaction Explorer
**Route:** `/gl-review/:runId/entries`

Features:
- Full-width paginated table (50 rows default, up to 200)
- **Filter bar:** risk tier chips, status filter, account dropdown, anomaly type, date range, search
- **Sortable columns:** date, amount, riskScore
- Per-row: risk score badge (0–100 color gradient), tier chip, account, party, description, date, debit/credit amounts
- Expandable row drawer:
  - Per-detector anomaly breakdown (from `gl_anomalies`)
  - AI explanation text (plain English)
  - `suggestedAction` and `accountingRiskType` from structured LLM response
  - `proposedCorrection` if LLM suggested one
  - Feedback controls: Confirm True Positive / Dismiss False Positive / Escalate
  - Assign to / add resolution notes
- **Status cycling:** Pending → Reviewed → Flagged (keyboard shortcut)
- **Bulk actions:** Select multiple → bulk mark as reviewed/flagged

#### `glReview/GLAnomalyAnalysis.tsx` — Anomaly Deep-Dive
**Route:** `/gl-review/:runId/anomaly`

Features:
- **Anomaly breakdown chart:** Bar chart of count per anomaly type (from `/analytics/anomaly-breakdown`)
- **Benford's Law panel:** Per-account first-digit distribution chart (actual vs expected)
- **Entity risk table:** Vendor risk ranking (from `/analytics/entity-risk`) with sorting
- **Anomaly detail drilldown:** Click any anomaly type → filtered entry list for that type
- **Tier-2 ML results panel:** Isolation Forest scores, DBSCAN cluster assignments, association rule rarities

#### `glReview/GLPeriodComparison.tsx` — Period & Trend Analysis
**Route:** `/gl-review/:runId/period`

Features:
- **Monthly trend chart:** Line/area chart of flaggedCount and avgRiskScore per month
- **Volume breakdown:** Total entries per month with flagged overlay
- **Risk shift:** Month-over-month change in tier distribution
- **Material exposure trend:** Total amount at risk per month
- Period selector (fiscal year quarters derived from `fiscalYearStartMonth`)

#### `glReview/GLAIAssistant.tsx` — AI Chat
**Route:** `/gl-review/:runId/chat`

Features:
- Multi-turn chat interface with server-persisted history (`gl_chat_messages`)
- AI model selector (dropdown: configured provider + model from settings)
- Context-aware: system prompt includes run analytics summary
- Suggested starter questions
- Message timestamps + copy-to-clipboard per message
- Available only for completed runs; shows "processing" placeholder while run is active

#### `glReview/GLSettings.tsx` — Configuration
**Route:** `/gl-review/settings`

Features:
- **Risk thresholds:** Editable slider/input for Critical/High/Medium/Low cutoffs
- **Tier weights:** Four weight sliders (auto-display sum; warn if not = 1.0)
- **Detector toggles:** Switches for each Tier-1 and Tier-2 enable flag
- **LLM configuration:** Provider selector, model input, API key fields (masked), "Test Key" button
- **Alerts:** Email toggle + address field; Slack webhook URL
- **Fiscal year:** Month selector (1–12)
- **Related parties:** Add/remove list of `{name, relationship}` pairs
- **Business rules table:** List with active toggle, edit, delete; "Add Rule" → inline form
  - Rule type selector → condition builder per type
  - Severity selector + description field

#### `glReview/GLReports.tsx` — Report Management
**Route:** `/gl-review/reports`

Features:
- **Scheduled reports list:** Table of configured report jobs (name, type, format, frequency, recipients, status)
- **Create report schedule:** Form with all options
- **Manual generate button:** Trigger immediate report generation for the current run
- **Report history:** Past generated reports with download links

---

### 13.2 Components & Context

**Directory:** `src/components/glReview/`

#### `GLReviewContext.tsx`
React context providing shared state across all GL Review pages:
```typescript
{
  currentRunId: number | null;
  currentRun: GlRun | null;
  runs: GlRun[];
  refreshRuns: () => void;
  settings: GlSettings | null;
}
```
Implements run polling (every 3s for `pending`/`processing` runs) and cache invalidation on status change.

#### `GLMetricCard.tsx`
KPI card with icon, label, value, and optional sublabel. Color variants for critical/high/medium/low.

#### `GLReviewTable.tsx`
Generic paginated data table with column sorting, row selection, expandable rows. Used by GLTransactionExplorer.

#### `GLRunSelectorBar.tsx`
Dropdown bar showing all runs for user; allows switching run context without navigating back to home.

#### `GLStatusBadge.tsx`
Status indicator with icon:
- `idle` — grey dot
- `uploading` — blue spinner
- `queued` — yellow dot
- `running` / `processing` — animated blue spinner + progress %
- `completed` / `done` — green checkmark
- `failed` — red X with error tooltip

#### `GLReviewPageSkeleton.tsx`
Loading skeleton matching the layout of each GL Review page. Shown while data is fetching after navigation.

---

## 14. Type Contracts

### Frontend Types (`src/types/glReview.ts`)

```typescript
type RiskTier = 'critical' | 'high' | 'medium' | 'low' | 'normal';
type RunStatus = 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
type LlmProvider = 'anthropic' | 'openai' | 'gemini' | 'ollama' | 'disabled';
type FeedbackStatus = 'pending' | 'confirmed_true_positive' | 'dismissed_false_positive';
type ResolutionStatus = 'unresolved' | 'resolved' | 'escalated';
type SuggestedAction = 'investigate' | 'verify_documentation' | 'approve' | 'escalate_to_cfo' | 'mark_normal';
type AccountingRiskType =
  | 'fraud_indicator' | 'duplicate_payment' | 'misclassification'
  | 'cutoff_error' | 'unauthorized_transaction' | 'data_entry_error'
  | 'policy_violation' | 'unusual_timing' | 'normal';
type MaterialityAssessment = 'material' | 'borderline' | 'immaterial';

interface GlRun {
  id: number;
  userId: number;
  status: RunStatus;
  fileName: string;
  fileType: string;
  fileSizeBytes: number;
  dateRangeFrom: string;
  dateRangeTo: string;
  totalEntries: number;
  flaggedCount: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  normalCount: number;
  avgRiskScore: number;
  progressPercentage: number;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  dataProfile?: DataProfile;
  materialExposure?: number;
  sourceFormat: 'quickbooks' | 'generic';
}

interface GlEntry {
  id: number;
  runId: number;
  rowIndex: number;
  date: string;
  journalRef: string;
  txnType: string;
  account: string;
  accountType: string;
  debit?: number;
  credit?: number;
  balance?: number;
  description: string;
  party: string;
  postedBy: string;
  riskScore: number;            // 0–100 integer
  riskTier: RiskTier;
  anomalyFlags: string[];
  aiExplanation?: string;
  riskExplanation?: LlmRiskExplanation;
  status: 'pending' | 'reviewed' | 'flagged';
  anomalyDetails?: GlAnomaly[]; // Per-detector breakdown
}

interface GlAnomaly {
  anomalyType: string;
  detectorScore: number;
  riskReasons: string[];
  metadata?: Record<string, unknown>;
}

interface LlmRiskExplanation {
  suggestedAction: SuggestedAction;
  actionDetail: string;
  accountingRiskType: AccountingRiskType;
  materialityAssessment: MaterialityAssessment;
  contextualRiskBoost: number;
  missingReversal: boolean;
  proposedCorrection?: ProposedCorrection;
}

interface ProposedCorrection {
  suggestedAccount: string;
  reason: string;
  memo: string;
  lines: Array<{
    account: string;
    debit?: number;
    credit?: number;
    description: string;
  }>;
}

interface GlSettings {
  thresholdCritical: number;
  thresholdHigh: number;
  thresholdMedium: number;
  thresholdLow: number;
  weightTier1Statistical: number;
  weightTier2Ml: number;
  weightTier3Rules: number;
  weightTier4Llm: number;
  llmProvider: LlmProvider;
  llmModel: string;
  enableBenfordLaw: boolean;
  enableRoundNumber: boolean;
  enableThresholdBreach: boolean;
  enableBackdating: boolean;
  enablePeriodEndCluster: boolean;
  enableFraudPatterns: boolean;
  enableNearDuplicates: boolean;
  enableIsolationForest: boolean;
  enableDbscan: boolean;
  enableAssociationRule: boolean;
  enableCopod: boolean;
  enableEcod: boolean;
  enableBehaviorProfiling: boolean;
  scheduledEnabled: boolean;
  scheduledFrequency?: 'daily' | 'weekly' | 'monthly';
  scheduledDayOfWeek?: number;
  scheduledHour?: number;
  emailAlerts: boolean;
  alertEmail?: string;
  slackWebhook?: string;
  fiscalYearStartMonth: number;
  relatedPartyList: Array<{ name: string; relationship: string }>;
}

interface GlBusinessRule {
  id: number;
  userId: number;
  ruleName: string;
  description: string;
  condition: FieldCondition | AccountThresholdCondition;
  severity: RiskTier;
  isActive: boolean;
}
```

---

## 15. Migration History

**Range:** `0062` through `0081+`

| Migration | Changes |
|---|---|
| `0062_gl_review_tables.sql` | Initial schema: `gl_runs`, `gl_entries`, `gl_anomalies`, `gl_settings` |
| `0063_*` | Enhanced `gl_settings`: tier weights, per-detector flags, LLM model field |
| `0064_*` | Add `txnType`, `accountType`, `balance` to `gl_entries` |
| `0065_*` | Add new anomaly type enum values |
| `0066_*` | Create `gl_scheduled_reports` table |
| `0067_*` | Create `gl_chat_messages` table |
| `0068_*` | Create `gl_feedback` table with resolutionStatus, assignedTo |
| `0069_*` | Create `gl_business_rules` table |
| `0070_*` | Add `dataProfile`, `materialExposure`, `sourceFormat` to `gl_runs` |
| `0071_*` | Add `riskExplanation` JSONB to `gl_entries` |
| `0072_*` | Add `apiKeyAnthropic`, `apiKeyOpenai`, `apiKeyGoogle` to `gl_settings` |
| `0073_*` | Add `slackWebhook` and `scheduledDayOfWeek`, `scheduledHour` to `gl_settings` |
| `0074_*` | Add `relatedPartyList` JSONB to `gl_settings` |
| `0075_*` | Add `queueJobId` to `gl_runs`; add `progressPercentage` |
| `0076_*` | Add `fileSizeBytes`, `fileType` to `gl_runs` |
| `0077_*` | Add `confidenceScore` to `gl_anomalies` |
| `0078_*` | Index additions: `(runId, riskTier)`, `(runId, anomalyType)` |
| `0079_*` | Add Tier-2 ML enable flags to `gl_settings` |
| `0080_*` | Add `enableBehaviorProfiling` flag |
| `0081_*` | Add `fiscalYearStartMonth` to `gl_settings`; cascade deletes for child tables |

**Total migrations:** ~20 (0062–0081)

---

## 16. End-to-End Data Flow

```
1. UPLOAD
─────────
User selects CSV/XLSX in GLReview.tsx
  → POST /api/gl-review/upload (multipart, max 50MB)
  → glFileParserService.parse(buffer, mimeType)
      ├─ Auto-detect delimiter / sheet
      ├─ Map column aliases → canonical fields
      ├─ Infer txnType from journalRef prefix
      └─ Infer accountType from account name
  → db.insert(gl_runs) → runId
  → db.insert(gl_entries, rows[]) [bulk]
  → glReviewQueue.add({ runId, userId })
  → Response: { runId, totalEntries, detectedColumns }

2. ASYNC ANALYSIS (glReviewWorker)
───────────────────────────────────
Load gl_settings for userId (create defaults if none)
Load gl_entries for runId → entries[]

[Phase 2] Data profiling → gl_runs.dataProfile, materialityThreshold

[Phase 3] Tier-1 (32 detectors):
  Group A (fast): duplicate, near_duplicate, z_score_outlier, iqr_outlier,
    temporal_z_score, benford_law, last_digit_pattern, round_number,
    amount_clustering, weekend_posting, holiday_posting, backdating,
    future_date, period_end_cluster, transaction_burst
  [event loop yield]
  Group B (grouped): split_transaction, reversal, threshold_breach,
    one_time_vendor, dormant_vendor, duplicate_invoice,
    excessive_journal_entries, rare_transaction_type,
    suspicious_keyword, fraud_pattern, journal_balance,
    data_quality, description_mismatch, account_velocity,
    unusual_account_pairing, recurring_pattern, related_party

[Phase 4] Tier-2 (ML, Promise.all, sample ≤3000):
  isolationForest, dbscan, association_rule, copod,
  ecod, account_behavior, entity_behavior

[Phase 5] Tier-3 (Business Rules):
  Load active gl_business_rules for userId
  Evaluate each entry against each rule
  Score = SEVERITY_SCORES[rule.severity]

[Phase 6] Pre-score (no LLM):
  For each entry: aggregate signals with tier weights → rawScore × 3.5
  riskScore = round(rawScore × 100)
  Select entries where riskTier ∈ {critical, high} AND !isGstEntry
  Cap at 100 entries → LLM candidates

[Phase 7] Tier-4 (LLM):
  For each batch of 5 LLM candidates:
    Build context payload (amount, account, party, flags, journalContext, materialityThreshold)
    POST to Anthropic / OpenAI / Gemini API
    Parse structured JSON response
    Extract: riskScore, explanation, suggestedAction, accountingRiskType,
             materialityAssessment, contextualRiskBoost, proposedCorrection
  [4 concurrent batches, 60s timeout each]

[Phase 8] Final scoring:
  Merge LLM signals back into detector signals
  Recompute all entries with full 4-tier weights
  Final riskScore (0–100) + riskTier for every entry

[Phase 9] Bulk write:
  UPDATE gl_entries SET riskScore, riskTier, anomalyFlags,
    aiExplanation, riskExplanation WHERE id IN (...)
  INSERT INTO gl_anomalies (one row per entry per fired detector)
  UPDATE gl_runs SET status='completed', flaggedCount, criticalCount,
    highCount, mediumCount, lowCount, normalCount, avgRiskScore,
    progressPercentage=100, completedAt=now()

3. NOTIFICATIONS
─────────────────
After Phase 9:
  if emailAlerts + alertEmail configured → emailService.sendRunSummary()
  if slackWebhook configured → slackAlertService.postRunSummary()

4. FRONTEND CONSUMPTION
────────────────────────
GLReview.tsx polls GET /runs every 5s while any run is pending/processing
On completed: user clicks → GLDashboard.tsx

GLDashboard: GET /runs/:id → counts, avgRiskScore, materialExposure

GLTransactionExplorer: GET /runs/:id/entries?riskTier=critical&page=1
  Auditor reviews entry, sees anomalyDetails + aiExplanation
  Clicks "Confirm True Positive"
  → PUT /runs/:id/entries/:entryId/feedback
  → db.upsert(gl_feedback, { status: 'confirmed_true_positive', ... })

GLAIAssistant: User types question
  → POST /nlquery (or chat route)
  → LLM called with run analytics context
  → Response streamed to chat bubble
  → Saved to gl_chat_messages

5. SCHEDULED RUNS
──────────────────
Scheduler (60s poll):
  Checks each user's gl_settings.scheduledEnabled
  If due (frequency + dayOfWeek + hour):
    Re-use last completed run's file reference
    Creates new gl_runs + gl_entries
    Enqueues glReviewWorker job
```

---

## 17. Key Design Decisions & Rationale

### 17.1 Score Range 0–100 (Integer) vs 0–1 (Float)

**Decision:** Final `riskScore` is stored and displayed as an integer 0–100.

**Rationale:** Business users (accountants, CFOs) relate more intuitively to a 0–100 "risk score" (like a credit score) than a 0.71 float. Thresholds (65, 40, 20, 8) are also more meaningful in this scale. The integer representation also simplifies sorting, filtering, and progress bar display.

### 17.2 Weighted Average + Amplifier vs Noisy-OR

**Decision:** Use `min(1, Σ(wᵢ × sᵢ / N) × 3.5)` instead of Noisy-OR.

**Rationale:** Noisy-OR compounds scores multiplicatively, making it hard for users to reason about how their configured tier weights affect the output. The linear weighted average is more interpretable: "Tier-1 contributes 25%, Tier-2 contributes 35%." The SCORE_AMPLIFIER (3.5) is a single calibration dial that can be tuned without changing the fundamental formula. The amplifier compensates for the fact that only a fraction of detectors fire for any given entry.

### 17.3 Tier Weights Configurable in Settings

**Decision:** All four tier weights are exposed in `gl_settings` and configurable per user.

**Rationale:** Different audit contexts need different emphasis. An organization primarily concerned with ML-detectable patterns might set Tier-2 to 0.60. One relying heavily on LLM judgment might set Tier-4 to 0.40. The weights auto-normalize to 1.0, so users can adjust one and the system remains valid.

### 17.4 LLM Only for Critical+High (Max 100/Run)

**Decision:** Tier-4 LLM only analyzes entries that pre-score to critical or high, capped at 100 entries per run.

**Rationale:**
1. **Cost:** LLM calls are the most expensive operation. Processing all entries for large GL files (10K+ rows) at batch size 5 would require 2,000+ API calls.
2. **Signal quality:** LLM adds the most value on entries already suspicious from statistical/ML detection. Running LLM on clearly normal entries wastes tokens and produces low-value output.
3. **Latency:** The 100-entry cap with 4 concurrent batches means Tier-4 completes in ≈ 20 × 60s batches ÷ 4 concurrency = ~5 minutes worst case. Beyond 100, the worker would run too long.

### 17.5 GST/Tax Exclusion from LLM

**Decision:** Entries matching GST/tax patterns are excluded from Tier-4 LLM analysis.

**Rationale:** Tax-related entries naturally exhibit patterns that trigger statistical flags (large round amounts, month-end clustering, government entity names). LLM models may describe these as suspicious. Since tax entries are regulated by strict statutory schedules, not business-decision anomalies, including them in LLM analysis produces false positives and undermines auditor trust in the AI explanations.

### 17.6 Upsert Feedback (vs Append-Only)

**Decision:** `gl_feedback` uses upsert — one row per entry, updated when auditor changes verdict.

**Rationale:** The use case is transactional auditing, not evidence chain-of-custody. Auditors frequently revise verdicts as they gather more information. Upsert simplifies queries (no "latest by created_at" logic needed) and reduces table bloat in large runs. The `reviewedAt` timestamp captures the most recent decision time. For SOX/PCAOB compliance where immutable audit history is critical, the append-only pattern would be preferable — this is a product-level design choice prioritizing simplicity over audit immutability.

### 17.7 Structured LLM Response Schema

**Decision:** LLM returns a structured JSON object with 10+ typed fields rather than plain text.

**Rationale:** Plain text explanations are useful for display but not for downstream logic. The structured response enables:
- `suggestedAction` → pre-populates the feedback action dropdown
- `accountingRiskType` → drives filtering/grouping in GLAnomalyAnalysis
- `materialityAssessment` → contributes to `materialExposure` KPI
- `proposedCorrection` → gives auditors an immediately actionable correction entry
- `contextualRiskBoost` → allows LLM to bump score for context it observes (e.g., this vendor just changed ownership) without overriding the statistical model entirely

### 17.8 In-Process Fallback for Queue

**Decision:** Bull queue falls back to in-process execution if Redis is unavailable.

**Rationale:** Allows the application to function in development environments or constrained deployments without a Redis dependency. The fallback runs the worker synchronously (blocking the upload response), acceptable for small files in dev. Production requires Redis for proper async behavior and job persistence.

### 17.9 Stuck Run Detection on Frontend

**Decision:** Frontend detects "stuck" runs (processing > 30 minutes) and surfaces a Retry option.

**Rationale:** Network interruptions, process crashes, or very large files can leave runs in `processing` state indefinitely. Since there's no heartbeat mechanism from the worker back to the DB, the frontend provides a safety valve. The Retry button calls `POST /runs/:id/retry` which resets status to `pending` and re-enqueues the job.

---

## 18. Complete File Index

### Backend — Services (Detector Implementations)

| File | Purpose |
|---|---|
| `server/services/glReview/glFileParserService.ts` | CSV/XLSX parsing + column auto-detection |
| `server/services/glReview/riskScoringService.ts` | Score fusion + tier classification |
| `server/services/glReview/llmNarrativeService.ts` | Tier-4 LLM narration + structured JSON response |
| `server/services/glReview/businessRulesService.ts` | Tier-3 rule evaluation engine |
| `server/services/glReview/glSettingsService.ts` | Settings read/write + defaults + legacy threshold migration |
| `server/services/glReview/isolationForestDetectionService.ts` | Isolation Forest ML detector |
| `server/services/glReview/dbscanDetectionService.ts` | DBSCAN clustering ML detector |
| `server/services/glReview/associationRuleDetectionService.ts` | Apriori/association rule ML detector |
| `server/services/glReview/copodDetectionService.ts` | COPOD ML detector |
| `server/services/glReview/ecodDetectionService.ts` | ECOD ML detector |
| `server/services/glReview/accountBehaviorDetectionService.ts` | Account behavioral profiling detector |
| `server/services/glReview/entityBehaviorDetectionService.ts` | Entity/vendor behavioral profiling detector |
| `server/services/glReview/emailService.ts` | SMTP email sender |
| `server/services/glReview/slackAlertService.ts` | Slack webhook notifier |
| `server/services/glReview/reportGenerationService.ts` | PDF/XLSX/CSV scheduled report generator |
| `server/services/glReview/scheduledRunService.ts` | Scheduled run trigger logic (60s poll) |

### Backend — Core

| File | Purpose |
|---|---|
| `server/workers/glReviewWorker.ts` | Main Bull job handler — 9-phase pipeline |
| `server/routes/gl-review/index.ts` | Route registration + auth middleware mount |
| `server/storage/gl-review.ts` | All DB access (Drizzle ORM queries) |

### Shared

| File | Purpose |
|---|---|
| `shared/schema/tables-gl-review.ts` | Drizzle ORM table definitions (8 tables) |

### Frontend — Pages

| File | Purpose |
|---|---|
| `src/pages/GLReview.tsx` | Home: upload + run list + stuck detection |
| `src/pages/glReview/GLDashboard.tsx` | Executive dashboard with KPIs + charts |
| `src/pages/glReview/GLTransactionExplorer.tsx` | Paginated explorer with feedback controls |
| `src/pages/glReview/GLAnomalyAnalysis.tsx` | Anomaly breakdown + entity risk + Benford charts |
| `src/pages/glReview/GLPeriodComparison.tsx` | Monthly trend + period comparison analytics |
| `src/pages/glReview/GLAIAssistant.tsx` | Persistent multi-turn AI chat per run |
| `src/pages/glReview/GLSettings.tsx` | Thresholds, weights, detectors, LLM, alerts, rules |
| `src/pages/glReview/GLReports.tsx` | Scheduled report management |

### Frontend — Components & Context

| File | Purpose |
|---|---|
| `src/components/glReview/GLMetricCard.tsx` | KPI card with icon + color variant |
| `src/components/glReview/GLReviewTable.tsx` | Generic paginated sortable table |
| `src/components/glReview/GLRunSelectorBar.tsx` | Run selector dropdown |
| `src/components/glReview/GLStatusBadge.tsx` | Animated status badge with progress % |
| `src/components/glReview/GLReviewPageSkeleton.tsx` | Per-page loading skeleton |
| `src/contexts/GLReviewContext.tsx` | Shared state: currentRun, runs list, settings, poller |

### Database

| File | Purpose |
|---|---|
| `migrations/0062_gl_review_tables.sql` | Initial schema |
| `migrations/0063_*.sql` through `migrations/0081_*.sql` | Incremental schema evolution (~20 migrations) |

### Documentation

| File | Purpose |
|---|---|
| `GL-Review-User-Guide.md` | End-user guide for the GL Review feature |

---

*End of GL Review Implementation Report (feat/GL-review branch)*

**Summary counts:**
- Tier-1 detectors: **32**
- Tier-2 ML detectors: **7**
- Tier-3 rule engine: **1** (unlimited instances)
- Tier-4 LLM detectors: **1**
- Database tables: **8**
- API routes: **25+**
- Frontend pages: **8**
- Migrations: **~20** (0062–0081)
- Score range: **0–100** (integer)
- Risk tiers: **5** (Normal/Low/Medium/High/Critical)
- Default thresholds: **8 / 20 / 40 / 65**
- Default LLM: **Anthropic claude-sonnet-4-6**