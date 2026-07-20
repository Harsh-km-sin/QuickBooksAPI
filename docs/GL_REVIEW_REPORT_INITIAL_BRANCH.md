# GL Review Feature — Implementation Report (Initial Branch)
**Journal Entry Analysis & Anomaly Detection System**

> **Document purpose:** AI-ready, exhaustive technical reference for the GL Review feature as implemented on the initial development branch.  
> **Project root:** `accounting-automations/accounting-automations/`  
> **Score range:** 0.0–1.0 float → risk tiers via fixed thresholds  
> **Date scanned:** 2026-06-24

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Architecture Overview](#2-system-architecture-overview)
3. [Database Schema](#3-database-schema)
4. [Data Normalization Pipeline](#4-data-normalization-pipeline)
5. [Detector Pipeline — Four-Tier Framework](#5-detector-pipeline--four-tier-framework)
   - 5.1 [Tier 1 — Statistical Baseline Detectors (15 Detectors)](#51-tier-1--statistical-baseline-detectors-15-detectors)
   - 5.2 [Tier 2 — Machine Learning Detectors (3 Detectors)](#52-tier-2--machine-learning-detectors-3-detectors)
   - 5.3 [Tier 3 — Business Rule Engine](#53-tier-3--business-rule-engine)
   - 5.4 [Tier 4 — AI Explanations (Best-Effort)](#54-tier-4--ai-explanations-best-effort)
6. [Score Fusion — Weighted Noisy-OR](#6-score-fusion--weighted-noisy-or)
7. [Feature Extraction (ML Layer)](#7-feature-extraction-ml-layer)
8. [Worker & Queue Architecture](#8-worker--queue-architecture)
9. [Backend API Routes](#9-backend-api-routes)
10. [Analytics Computation Engine](#10-analytics-computation-engine)
11. [Storage Layer](#11-storage-layer)
12. [Frontend Architecture](#12-frontend-architecture)
    - 12.1 [Pages](#121-pages)
    - 12.2 [Components](#122-components)
    - 12.3 [Client API Library](#123-client-api-library)
13. [Type Contracts](#13-type-contracts)
14. [Database Migration History](#14-database-migration-history)
15. [End-to-End Data Flow](#15-end-to-end-data-flow)
16. [Key Design Decisions & Rationale](#16-key-design-decisions--rationale)
17. [Complete File Index](#17-complete-file-index)

---

## 1. Executive Summary

The **GL Review** feature (initial branch) is a multi-tiered General Ledger auditing system that ingests journal entry exports from any ERP (SAP, NetSuite, Xero, QuickBooks) and applies layered anomaly detection to surface suspicious or erroneous transactions for auditor review.

### Core Capabilities

| Capability | Description |
|---|---|
| **File Ingestion** | CSV or XLSX upload with automatic ERP schema normalization |
| **Statistical Detection** | Z-Score outliers, duplicate detection, threshold structuring, Benford's Law |
| **Temporal Detection** | Backdating, period-end posting, weekend/holiday posting |
| **ML Detection** | Isolation Forest, DBSCAN clustering, Apriori cross-run itemsets (opt-in via `enableMl` flag) |
| **Business Rules** | User-configurable rules: restricted accounts, vendor blacklists, amount ranges, approval thresholds by user, day-of-week controls |
| **AI Narration** | Per-transaction natural language explanations + executive summary via LLM (best-effort, never fails the run) |
| **Auditor Feedback** | Append-only true positive / false positive / needs-review verdicts per transaction |
| **Analytics Dashboard** | KPIs, risk distribution, Benford charts, monetary flows, period-over-period comparison |
| **AI Chat** | Multi-turn conversational assistant with analytics context injection (client-side history only) |
| **Email Notifications** | Completion alerts for runs with critical/high findings |

### Technology Stack

| Layer | Technology |
|---|---|
| Frontend | React + TypeScript, Recharts, TanStack Query |
| Backend | Node.js + Express, TypeScript |
| Database | PostgreSQL (Drizzle ORM) |
| Queue | Bull (Redis-backed) |
| AI/LLM | Gemini, OpenAI GPT-4o, Qwen (via Fireworks) |
| ML | Pure TypeScript: Isolation Forest, DBSCAN, Apriori |
| File Parsing | `xlsx` library (XLSX/CSV) |
| Schema Validation | Zod |
| Email | Nodemailer / transactional email provider |

---

## 2. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (React)                         │
│  Upload → Dashboard → Transactions → Anomaly → Compare → Chat   │
└────────────────────────┬────────────────────────────────────────┘
                         │ REST API (/api/gl-review/*)
┌────────────────────────▼────────────────────────────────────────┐
│                   Express API Server                             │
│  POST /upload  GET /runs  GET /transactions  POST /chat  ...    │
└────────────────────────┬────────────────────────────────────────┘
                         │
           ┌─────────────┴──────────────┐
           │                            │
┌──────────▼──────────┐    ┌────────────▼────────────┐
│  glReviewWorker      │    │  glReviewNotifyWorker    │
│  (Bull Queue Job)    │    │  (Bull Queue Job)        │
│                      │    │                          │
│  1. Load txns        │    │  Send email to           │
│  2. Extract ML       │    │  notifyEmail address     │
│     features         │    │  if critical/high found  │
│  3. Run detectors    │    └──────────────────────────┘
│     ├─ Tier-1 stat   │
│     ├─ Tier-2 ML     │
│     ├─ Tier-3 rules  │
│     └─ Noisy-OR      │
│  4. Write results    │
│  5. AI explanations  │
│  6. Exec summary     │
│  7. Mark completed   │
└──────────┬──────────┘
           │
┌──────────▼──────────────────────────────────────────────────────┐
│                     PostgreSQL Database                          │
│  gl_review_runs · gl_review_transactions · gl_review_results    │
│  gl_review_rules · gl_review_feedback · gl_review_features      │
└─────────────────────────────────────────────────────────────────┘
```

### Layered Detection Model

```
Every GL row passes through all active tiers simultaneously:

Tier 1 (Always-on Statistical):
  zscore, duplicate, threshold, backdating, roundNumber,
  periodEnd, benford, weekend, dcImbalance, suspenseAccount,
  negativeReversal, unusualUser, holidayPosting, taxAnomaly,
  missingDescription

Tier 2 (Opt-in ML, enableMl flag):
  isolationForest, dbscan, apriori

Tier 3 (User Business Rules → makeRuleDetector()):
  business_rule_breach (one closure detector per user rule set)

Score Fusion:
  Weighted Noisy-OR:  composite = 1 − ∏(1 − clamp(wᵢ · sᵢ))
  scoreToTier():      maps composite [0,1] → Normal/Low/Medium/High/Critical
  Tier Floor:         max(scoreToTier(composite), rule.tierFloor)
```

---

## 3. Database Schema

**File:** `shared/schema/tables-gl-review.ts`  
**Migration range:** `0057` – `0065`

### 3.1 `gl_review_runs`

Metadata record for each analysis job. One row per uploaded file.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | Auto-increment job ID |
| `userId` | integer FK | Owner (auth user) |
| `status` | enum | `pending` \| `running` \| `completed` \| `failed` |
| `fileName` | text | Original uploaded file name |
| `rowCount` | integer | Total GL rows parsed |
| `flaggedCount` | integer | Rows with riskTier ≠ normal |
| `analysisType` | text | Reserved (future: 'standard' \| 'forensic') |
| `startedAt` | timestamp | Worker start time |
| `completedAt` | timestamp | Worker finish time |
| `errorMessage` | text | Failure reason (if status=failed) |
| `aiProvider` | text | `GEMINI` \| `OPENAI` \| `QWEN` |
| `aiExplanationsEnabled` | boolean | Whether per-row AI text was requested |
| `aiExecutiveSummary` | text | Markdown summary generated by AI |
| `notifyEmail` | text | Email to alert on completion |
| `notifiedAt` | timestamp | When alert was sent |
| `notifyStatus` | text | `sent` \| `not_sent` \| `no_critical_high` |
| `enableMl` | boolean | Whether Tier-2 ML detectors ran |
| `fiscalYearStartMonth` | integer | 1–12, for period comparison slicing |

### 3.2 `gl_review_transactions`

Canonical GL row store. One row per journal line item.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `runId` | integer FK → gl_review_runs(id) CASCADE DELETE | |
| `userId` | integer FK | Denormalized for fast per-user queries |
| `rowIndex` | integer | Original row position in uploaded file |
| `transactionId` | text | JE document number (from ERP) |
| `transactionDate` | date | Business / value date |
| `createdDate` | date | System entry date (audit stamp) |
| `accountId` | text | Chart of Accounts code |
| `accountName` | text | Full account name |
| `accountType` | text | Asset \| Liability \| Equity \| Revenue \| Expense |
| `postingType` | text | `Debit` \| `Credit` |
| `amount` | numeric | Absolute value (sign stripped) |
| `currency` | text | ISO 4217 code |
| `entityName` | text | Vendor / counterparty name |
| `description` | text | Narration / memo |
| `sourceType` | text | ERP source module (AR, AP, GL, etc.) |
| `createdBy` | text | System user who posted the entry |

**Indexes:** `(runId)`, `(userId)`, `(runId, rowIndex)`

### 3.3 `gl_review_results`

Detector output per transaction. One row per `gl_review_transactions` row.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `runId` | integer FK CASCADE DELETE | |
| `transactionId` | integer FK → gl_review_transactions(id) | Links to source row |
| `compositeRiskScore` | numeric | Fused score **[0.00–1.00]** |
| `riskTier` | enum | `normal` \| `low` \| `medium` \| `high` \| `critical` |
| `anomalyFlags` | JSONB | `string[]` — list of flag codes triggered |
| `zscoreValue` | numeric | Raw Z-Score from zscore detector |
| `detectorScores` | JSONB | `Record<string, {score, evidence, flag}>` per detector |
| `aiExplanation` | text | LLM-generated natural language explanation |
| `suggestedAction` | text | LLM-suggested auditor action |

### 3.4 `gl_review_rules`

User-defined business rules persisted as structured JSON.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `userId` | integer FK | Rule owner |
| `name` | text | Human label |
| `ruleType` | text | `restricted_account` \| `restricted_vendor` \| `amount_range` \| `approval_threshold_by_user` \| `day_of_week` |
| `conditionJson` | JSONB | Rule parameters (type-specific schema) |
| `severity` | enum | `low` \| `medium` \| `high` \| `critical` — sets `tierFloor` |
| `action` | text | Human description of desired action |
| `isActive` | boolean | Whether rule is applied in new runs |
| `createdAt` | timestamp | |
| `updatedAt` | timestamp | |

**Indexes:** `(userId)`, `(userId, isActive)`

### 3.5 `gl_review_feedback`

Auditor verdicts — **append-only** (new row per verdict change).

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `runId` | integer FK | |
| `transactionRowId` | integer FK → gl_review_transactions(id) | |
| `userId` | integer FK | Auditor who submitted verdict |
| `verdict` | enum | `true_positive` \| `false_positive` \| `needs_review` |
| `notes` | text | Optional auditor notes |
| `createdAt` | timestamp | |

**Indexes:** `(runId, transactionRowId)`, `(userId)`

**Design note:** Current verdict = latest row by `createdAt`. The full history is preserved for future ML training on auditor decision patterns.

### 3.6 `gl_review_features`

ML feature matrix for each transaction. Computed once, reused by all Tier-2 detectors.

| Column | Type | Notes |
|---|---|---|
| `id` | serial PK | |
| `transactionId` | integer FK (unique) | One-to-one with gl_review_transactions |
| `isRoundAmount` | boolean | Multiple of $1K/$10K/$100K |
| `isWeekendPosting` | boolean | Saturday or Sunday |
| `firstDigit` | integer | Leading digit 1–9 (Benford input) |
| `isPeriodEnd` | boolean | Last day of month |
| `isBackdated` | boolean | createdDate − transactionDate > 1 day |
| `backdateDays` | integer | Days of lag |
| `accountMean` | numeric | Per-account average amount |
| `accountStddev` | numeric | Per-account standard deviation |
| `zscoreAmount` | numeric | Normalized Z-Score of amount |

---

## 4. Data Normalization Pipeline

**File:** `server/lib/glReviewNormalize.ts`

### 4.1 Supported Input Formats

**Format A — Flat ERP exports (SAP, NetSuite, Xero)**
- Row 1 = header; subsequent rows = data
- Column aliases auto-detected via fuzzy mapping

**Format B — QuickBooks hierarchical exports**
- Rows 1–4 = report metadata + blank lines; real header at ~row 5
- Account names appear as section-header rows (not in a dedicated column)
- Posting direction derived from amount sign (positive = debit, negative = credit)

### 4.2 Column Alias Resolution

| Canonical Field | Known Aliases |
|---|---|
| `transactionId` | `txn_id`, `je_number`, `journal_id`, `document_number`, `voucher` |
| `transactionDate` | `date`, `posting_date`, `value_date`, `txn_date` |
| `createdDate` | `entry_date`, `system_date`, `input_date`, `created_at` |
| `accountId` | `account_code`, `gl_code`, `acct_no`, `account_number` |
| `accountName` | `account`, `account_description`, `gl_account` |
| `amount` | `debit`, `credit`, `net_amount`, `value`, `local_amount` |
| `entityName` | `vendor`, `customer`, `supplier`, `counterparty`, `name` |
| `description` | `memo`, `narration`, `details`, `remarks` |
| `createdBy` | `user`, `posted_by`, `entered_by`, `preparer` |

### 4.3 Data Cleaning Rules

| Field | Cleaning Applied |
|---|---|
| `amount` | Strip `$`, `,`, `€`, `£`; convert `(1,234)` → `-1234`; absolute value stored |
| `transactionDate` | Try `DD/MM/YYYY` (QB international) then `MM/DD/YYYY` fallback |
| `postingType` | `D`, `DR`, `Debit` → `"Debit"`; `C`, `CR`, `Credit` → `"Credit"` |
| `accountType` | Regex inference: matches `Cost of`, `Revenue`, `Expense`, `Asset`, `Liability`, `Equity` |
| `currency` | Defaults to `USD` if absent |

### 4.4 Output: `NormalizedGlRow`

```typescript
interface NormalizedGlRow {
  rowIndex: number;
  transactionId: string;
  transactionDate: Date;
  createdDate: Date;
  accountId: string;
  accountName: string;
  accountType: string;
  postingType: 'Debit' | 'Credit';
  amount: number;     // Always positive
  currency: string;
  entityName: string;
  description: string;
  sourceType: string;
  createdBy: string;
}
```

---

## 5. Detector Pipeline — Four-Tier Framework

**File:** `server/lib/glReview/registry.ts`

All detectors implement a common interface, receive the full transaction array, and emit per-row signals via a `Map<rowIndex, DetectorSignal>`.

### Core Interface

```typescript
// server/lib/glReview/types.ts
interface Detector {
  name: string;
  weight: number;
  run(rows: DetectorInput[]): Map<number, DetectorSignal>;
}

interface DetectorSignal {
  score: number;        // [0, 1] raw signal strength
  flag: string;         // Short code e.g. "zscore_outlier"
  evidence: string;     // Human-readable justification
  tierFloor?: RiskTier; // Optional: guarantee minimum risk tier
}

interface DetectorResult {
  transactionId: number;
  compositeRiskScore: number;    // [0.00–1.00]
  riskTier: RiskTier;
  anomalyFlags: string[];
  zscoreValue?: number;
  detectorScores: Record<string, DetectorSignal>;
}
```

---

### 5.1 Tier-1 — Statistical Baseline Detectors (15 Detectors)

Always-on. Run on every analysis.

#### `zscore` — Statistical Amount Outlier
**Weight:** 0.90 (highest) | **Flag:** `zscore_outlier`  
**File:** `server/lib/glReview/detectors/zscore.ts`

- Groups by `accountName`; uses account peer group if ≥10 transactions, global otherwise
- `zScore = (amount − mean) / stddev`; flags if `|zScore| > 3.0`
- Score: `min(1.0, (|zScore| − 3.0) / 3.0)` — scales from 3σ to 6σ
- Evidence: `"Amount $X is Z.Yσ above account peer mean $M (n=N rows)"`

#### `duplicate` — Exact Duplicate Detection
**Weight:** 0.85 | **Flag:** `duplicate`  
**File:** `server/lib/glReview/detectors/duplicate.ts`

- Key: `accountName|amount.toFixed(2)|transactionDate|description`
- Any group with >1 row → all members flagged; score 1.0

#### `threshold` — Approval Threshold Structuring
**Weight:** 0.75 | **Flag:** `threshold_structuring`  
**File:** `server/lib/glReview/detectors/threshold.ts`

- Thresholds: `[$5K, $10K, $25K, $50K, $100K, $500K, $1M]`
- Flags: amount within 5% below a threshold (`(T − a) / T ≤ 0.05`)
- Score: `1 − ((T − a) / (T × 0.05))`; higher = closer to threshold

#### `backdating` — Post-Period Entry Detection
**Weight:** 0.70 | **Flag:** `backdated_entry`  
**File:** `server/lib/glReview/detectors/backdating.ts`

- `lag = createdDate − transactionDate` in days; flags if `lag > 1`
- Score: `min(1.0, lag / 30)`; caps at 30+ day lag

#### `dcImbalance` — Debit/Credit Imbalance
**Weight:** 0.70 | **Flag:** `dc_imbalance`  
**File:** `server/lib/glReview/detectors/dcImbalance.ts`

- Groups by `transactionId`; checks `Σdebits = Σcredits`
- Flags journals where `|debits − credits| > $0.01`
- Score: `min(1.0, imbalance / 1000)`

#### `roundNumber` — Suspiciously Round Amounts
**Weight:** 0.55 | **Flag:** `round_number`  
**File:** `server/lib/glReview/detectors/roundNumber.ts`

- `amount % 1000 === 0` → 0.5; `% 10000` → 0.7; `% 100000` → 0.9; `% 1000000` → 1.0

#### `periodEnd` — Close-of-Period Posting
**Weight:** 0.50 | **Flag:** `period_end_posting`  
**File:** `server/lib/glReview/detectors/periodEnd.ts`

- Flags entries posted on last day of month; score 1.0

#### `benford` — Benford's Law First-Digit Analysis
**Weight:** 0.45 | **Flag:** `benford_anomaly`  
**File:** `server/lib/glReview/detectors/benford.ts`

- Per-account (≥10 transactions); expected: `log₁₀(1 + 1/d)`
- Flags if any digit deviates >10% from expected; score proportional to deviation

#### `weekend` — Off-Hours Posting
**Weight:** 0.40 | **Flag:** `weekend_posting`  
**File:** `server/lib/glReview/detectors/weekend.ts`

- `transactionDate.getUTCDay() ∈ {0, 6}`; score 1.0

#### `suspenseAccount` — Temporary Clearing Account
**Weight:** 0.65 | **Flag:** `suspense_account`

- `accountName` matches known clearing/suspense keywords

#### `negativeReversal` — Unusual Reversal / Negative Entry
**Weight:** 0.60 | **Flag:** `negative_reversal`

- `amount` with opposite sign to account's typical `postingType`

#### `unusualUser` — Rare User Activity on Account
**Weight:** 0.55 | **Flag:** `unusual_user`

- `createdBy` rarely posts to this `accountName` (frequency analysis across run)

#### `holidayPosting` — Public Holiday Entry
**Weight:** 0.45 | **Flag:** `holiday_posting`

- `transactionDate` falls on public holiday (locale-aware via date library)

#### `taxAnomaly` — Tax Amount Misalignment
**Weight:** 0.50 | **Flag:** `tax_anomaly`

- Tax-related account amounts inconsistent with inferred taxable base

#### `missingDescription` — Blank Narration Field
**Weight:** 0.35 | **Flag:** `missing_description`

- `description` is null, empty, or <3 characters

---

### 5.2 Tier-2 — Machine Learning Detectors (3 Detectors)

**Opt-in:** Enabled by `enableMl: true` on the run.  
All three run on `gl_review_features` rows (pre-extracted, persisted).

#### `isolationForest` — Isolation Forest Anomaly Detection
**File:** `server/lib/glReview/detectors/isolationForest.ts`

**Hyperparameters:**
- Trees: 100 | Subsample: 256 | Max depth: ~8 (`ceil(log₂(256))`)
- Anomaly threshold: 0.62 | Minimum rows: 20

**Feature vector:**
```
[amount, zscoreAmount, backdateDays, isRoundAmount(0/1),
 isWeekendPosting(0/1), isPeriodEnd(0/1), isBackdated(0/1)]
```

- Anomaly score via Isolation Forest path-length formula: `2^(-E[h(x)] / c(n))`
- Signal score: `max(0, anomalyScore − 0.5) × 2`
- **Flag:** `isolation_forest_anomaly`

#### `dbscan` — Density-Based Clustering
**File:** `server/lib/glReview/detectors/dbscan.ts`

- Feature space: `(amount, zscoreAmount)`
- Noise points (no cluster assignment) = anomalous
- **Flag:** `dbscan_outlier`

#### `apriori` — Cross-Run Frequent Itemset Mining
**File:** `server/lib/glReview/detectors/apriori.ts`

- Pattern: `(accountName, amount_bucket)` pairs across current + prior runs
- Transactions that violate frequently-observed patterns are flagged
- Uses `gl_review_features` rows from prior runs for the same user
- **Flag:** `apriori_anomaly`

---

### 5.3 Tier-3 — Business Rule Engine

**File:** `server/lib/glReview/rules.ts`

User rules are loaded from `gl_review_rules` at job start and compiled into a single closure-based detector via `makeRuleDetector()`.

**Rule types and condition schemas (Zod-validated):**

| Rule Type | Condition Schema | Evaluation Logic |
|---|---|---|
| `restricted_account` | `{ accounts: string[] }` | `accountName ∈ accounts` |
| `restricted_vendor` | `{ vendors: string[] }` | `entityName ∈ vendors` |
| `amount_range` | `{ min?: number; max?: number }` | `min ≤ amount ≤ max` |
| `approval_threshold_by_user` | `{ thresholds: [{user, limit}] }` | `createdBy matches user AND amount > limit` |
| `day_of_week` | `{ days: number[] }` (0=Sun) | `transactionDate.getUTCDay() ∈ days` |

**Key function:** `makeRuleDetector(rules: GlReviewRule[]): Detector | null`

- Returns `null` if user has no active rules
- Weight: 0.80
- Score on match: `1.0` (deterministic, binary)
- `tierFloor` = `rule.severity` — guarantees a minimum risk tier regardless of composite score

---

### 5.4 Tier-4 — AI Explanations (Best-Effort)

**File:** `server/workers/glReviewWorker.ts` (AI section) + `server/lib/glReview/prompts.ts`

Runs after all detector results are written. Wrapped in try/catch — **never fails the run**.

**Providers:** `GEMINI` | `OPENAI` | `QWEN` (via Fireworks.ai)  
Resolved from `gl_review_runs.aiProvider` + user's API key store.

#### Two Explanation Paths

**Path A — Vendor Story (for vendors with ≥2 flagged rows + temporal variation)**
1. Build `VendorPatternProfile`: sorted history (date, amount, flags) per vendor
2. Detect "what changed" (spike, new account, timing shift)
3. One narrative per vendor: "This vendor historically posted $X–$Y monthly to [Account], but in [Month] the amount jumped to $Z with [flag]"
4. Fan out the same explanation to all flagged rows for that vendor
5. Batch size: 8 vendors per LLM call

**Path B — Flag-Only (per-row, when no vendor profile exists)**
1. Build row context: amount, account, flags, Z-Score, date
2. Generate per-row explanation referencing specific detector flags
3. Batch size: 10 rows per LLM call

**Markdown stripping:** Per-transaction `aiExplanation` strips `**bold**`, `` `code` ``, `#` headings, `- •` bullets → plain text.  
**Executive summary** (`aiExecutiveSummary`) retains markdown (rendered via `GlMarkdown.tsx`).

**System prompts used:**

| Prompt | Purpose |
|---|---|
| `VENDOR_STORY_SYSTEM` | Batch vendor temporal narrative |
| `FLAG_ONLY_SYSTEM` | Per-row flag explanation |
| `EXECUTIVE_SUMMARY_SYSTEM` | 3–4 paragraph markdown summary |
| `FLAG_GLOSSARY` | Plain-English definitions of each flag code, injected into all prompts |

---

## 6. Score Fusion — Weighted Noisy-OR

**File:** `server/lib/glReview/aggregate.ts`

### Formula

```
composite = 1 − ∏(1 − clamp(wᵢ · sᵢ, 0, 1))
```

Where:
- `wᵢ` = detector weight (e.g., 0.90 for `zscore`)
- `sᵢ` = detector signal score [0, 1]
- Product iterates over all detectors that emitted a signal for this row

**Properties:**
- Zero detectors fire → composite = 0
- Single detector at score 1.0, weight 0.9 → composite = 0.90
- Multiple detectors compound without simple addition (no >1.0 overflow)
- High-weight detectors dominate; low-weight detectors add marginally

### Risk Tier Boundaries (`scoreToTier`)

| Composite Score | Risk Tier |
|---|---|
| ≥ 0.85 | `critical` |
| ≥ 0.65 | `high` |
| ≥ 0.40 | `medium` |
| ≥ 0.15 | `low` |
| < 0.15 | `normal` |

**These thresholds are fixed** (not user-configurable in this implementation).

### Tier Floor from Rules

```
finalTier = max(scoreToTier(compositeScore), rulesTierFloor)
```

Ensures: a transaction matching a `critical`-severity business rule is always `critical`, regardless of composite score.

---

## 7. Feature Extraction (ML Layer)

**Files:**
- `server/lib/glReview/features/features.ts` — Entry point
- `server/lib/glReview/features/primitives.ts` — Reusable primitives

Features are computed **once per run** as a separate first pass, persisted to `gl_review_features`, then reused by all Tier-2 ML detectors and the Apriori cross-run analysis.

### Feature Definitions

| Feature | Type | Computation |
|---|---|---|
| `isRoundAmount` | boolean | `amount % 1000 === 0` |
| `isWeekendPosting` | boolean | `day ∈ {0, 6}` |
| `firstDigit` | 1–9 | First significant digit of `amount` |
| `isPeriodEnd` | boolean | Last day of the transaction month |
| `isBackdated` | boolean | `(createdDate − transactionDate) > 1 day` |
| `backdateDays` | integer | `max(0, createdDate − transactionDate)` |
| `accountMean` | numeric | Mean of `amount` for same `accountName` |
| `accountStddev` | numeric | Std deviation for same `accountName` |
| `zscoreAmount` | numeric | `(amount − accountMean) / accountStddev` |

### Primitives Library

```typescript
isRound(amount: number): { flag: boolean; score: number }
isWeekend(date: Date): boolean
firstDigit(amount: number): number         // Returns 1–9
isPeriodEnd(date: Date): boolean
isBackdated(txnDate: Date, createdDate: Date): { flag: boolean; days: number; score: number }
accountZscore(amount: number, mean: number, stddev: number): number
```

These primitives are also called directly by Tier-1 detectors (`backdating`, `roundNumber`, `periodEnd`, `benford`) to avoid duplicated calculation.

---

## 8. Worker & Queue Architecture

**Queue name:** `glReviewQueue` (Bull, Redis-backed)  
**Notify queue:** `glReviewNotifyQueue`

### `glReviewWorker.ts` — Main Analysis Worker

**Job payload:** `{ runId: number; userId: number }`

**Execution phases:**

| Phase | Description |
|---|---|
| **Phase 1: Load** | `getTransactionsForAnalysis(runId)` → `DetectorInput[]`; `markRunRunning(runId)` |
| **Phase 2: Feature Extraction** | `extractFeatures(rows)` → `writeFeatures(features)` — persisted for Tier-2 + cross-run Apriori |
| **Phase 3: Detector Pipeline** | Load active rules → `makeRuleDetector()`; build detector list; `registry.runDetectors()`; `writeResults()` |
| **Phase 4: AI (best-effort)** | If `aiExplanationsEnabled`: vendor story or flag-only path → `updateAiExplanations()`; then `generateExecutiveSummary()` → `updateAiExecutiveSummary()` |
| **Phase 5: Complete** | `markRunCompleted(runId, flaggedCount)`; enqueue notify job |

**Error handling:**
- Phases 1–3 failures → `markRunFailed(runId, errorMessage)`
- Phase 4 AI failures → logged; run still marked completed

### `glReviewNotifyWorker.ts` — Email Notification

1. Check `notifyEmail` is set
2. Check run has at least one critical/high entry (else set `notifyStatus = 'no_critical_high'`)
3. Send email: file name, total rows, flagged count, critical count, dashboard link
4. Update `notifiedAt` + `notifyStatus = 'sent'`

---

## 9. Backend API Routes

**Mount point:** `/api/gl-review/` (all routes auth-gated)

| Method | Path | File | Description |
|---|---|---|---|
| `POST` | `/upload` | `upload.ts` | Multipart CSV/XLSX upload; normalize; create run; enqueue worker |
| `GET` | `/runs` | `runs.ts` | List paginated runs |
| `GET` | `/runs/:runId` | `runs.ts` | Single run metadata |
| `DELETE` | `/runs/:runId` | `runs.ts` | Delete run + all child data |
| `GET` | `/runs/:runId/analytics` | `runs.ts` | Dashboard metrics + period comparison |
| `GET` | `/runs/:runId/transactions` | `transactions.ts` | Paginated, filtered transaction list |
| `GET` | `/runs/:runId/filter-options` | `transactions.ts` | Dynamic dropdown values from data |
| `POST` | `/runs/:runId/chat` | `chat.ts` | Multi-turn AI assistant message |
| `GET` | `/runs/:runId/export` | `export.ts` | CSV download of all transactions |
| `POST` | `/runs/:runId/transactions/:rowId/feedback` | `feedback.ts` | Append auditor verdict |
| `GET` | `/rules` | `rules.ts` | List user's business rules |
| `POST` | `/rules` | `rules.ts` | Create new rule |
| `PATCH` | `/rules/:ruleId` | `rules.ts` | Update rule (toggle / edit) |
| `DELETE` | `/rules/:ruleId` | `rules.ts` | Delete rule |

### `POST /upload` — Key Options

| Field | Type | Description |
|---|---|---|
| `file` | File | CSV or XLSX |
| `aiProvider` | string | `GEMINI` / `OPENAI` / `QWEN` |
| `aiExplanationsEnabled` | boolean | Enable per-row AI explanations |
| `notifyEmail` | string | Completion alert email |
| `enableMl` | boolean | Enable Tier-2 ML detectors |
| `fiscalYearStartMonth` | number | 1–12 for period comparison |

### `GET /runs/:runId/transactions` — Filter Params

| Param | Description |
|---|---|
| `riskTier` | `normal/low/medium/high/critical` |
| `account` | Exact `accountName` |
| `sourceType` | Source module filter |
| `postingType` | `Debit` / `Credit` |
| `flag` | Filter by anomaly flag code |
| `search` | Text search on description/entity/account |
| `startDate` / `endDate` | Date range |
| `page` / `pageSize` | Pagination (default 50, max 200) |

### `POST /runs/:runId/chat` — Request Body

```json
{
  "message": "Which accounts have the most critical entries?",
  "aiModel": "gemini-1.5-pro",
  "history": [
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ]
}
```

Context injected into prompt: run metrics, risk distribution, top 10 transaction samples, detector glossary. Chat history is maintained **client-side only**.

---

## 10. Analytics Computation Engine

**File:** `server/lib/glReviewAnalytics.ts`

All metrics are computed server-side from raw `gl_review_transactions` + `gl_review_results`.

| Metric / Dataset | Computation |
|---|---|
| **Core KPIs** | `totalTransactions`, `flagged`, `criticalHigh`, `avgRiskScore` |
| **Risk distribution** | `{ tier, count, percentage }[]` — used in pie chart |
| **Monthly trend** | Per month: `transactionCount`, `flaggedCount`, `highRiskCount` |
| **Top flagged rows** | Top 8 by `compositeRiskScore`; includes entity, account, amount, flags, AI snippet |
| **Benford data** | Per account (≥10 txns): `{ digit, actual%, expected%, deviation }[]` |
| **Monetary flows** | Debit→Credit account pairs with rarity score (inverse frequency) |
| **Entity risk** | Vendors sorted by aggregate risk: `{ entityName, maxRiskScore, flaggedCount, totalAmount }` |
| **Amount buckets** | Count distribution by range: <$100, $100–$1K, $1K–$10K, $10K–$100K, $100K–$1M, >$1M |
| **Detector breakdown** | Per-detector flagged count + percentage (donut chart data) |
| **Balance check** | `{ debitSum, creditSum, difference, status }` |
| **Posting timeline** | Per-date: `count`, `hasWeekend`, `hasHighRisk` |
| **Period comparison** | Prior vs current period metrics, account/entity/flow changes, change items |

### Period Comparison

When `fiscalYearStartMonth` is set on the run:

```
derivePeriods(transactions, fiscalYearStartMonth) → Period[]
Each Period: { label, start, end, transactionCount, flaggedCount }

Comparison output:
  metricsDelta       — { total, flagged, criticalHigh, avgScore } per period
  volumeByAccount    — prior/current count + delta% per account
  accountChanges     — { newAccounts, dormantAccounts }
  entityChanges      — { newEntities, dormantEntities }
  flowChanges        — { newFlows, dormantFlows }
  changeItems        — [{ type: 'new'|'spike'|'resolved', description }]
```

---

## 11. Storage Layer

**File:** `server/storage/gl-review.ts`

All database access goes through this layer (Drizzle ORM, parameterized queries).

| Function | Description |
|---|---|
| `createRun(userId, opts)` | Insert `gl_review_runs` row |
| `insertTransactions(runId, userId, rows[])` | Bulk insert chunked at **1000 rows/statement** (avoids >65K bind params) |
| `markRunRunning(runId)` | Set `status='running'`, `startedAt=now()` |
| `markRunCompleted(runId, flaggedCount)` | Set `status='completed'`, `flaggedCount`, `completedAt` |
| `markRunFailed(runId, errorMessage)` | Set `status='failed'`, `errorMessage` |
| `getTransactionsForAnalysis(runId)` | Load all transaction rows for detector pipeline |
| `writeResults(runId, results[])` | Bulk upsert `gl_review_results` |
| `writeFeatures(features[])` | Bulk insert `gl_review_features` |
| `getRunTransactions(runId, filters, page, size)` | Paginated filtered join query |
| `getRunAnalytics(runId, periodOpts?)` | Compute all dashboard metrics |
| `getTopResultsForExplanation(runId, limit)` | Top-N flagged rows for AI narration |
| `updateAiExplanations(updates[])` | Batch update `aiExplanation` + `suggestedAction` |
| `updateAiExecutiveSummary(runId, summary)` | Update `gl_review_runs.aiExecutiveSummary` |
| `appendFeedback(runId, rowId, userId, verdict, notes)` | Insert verdict row (append-only) |
| `listRulesForUser(userId)` | All rules for user |
| `createRule(userId, data)` | Insert new rule |
| `updateRule(ruleId, userId, patch)` | Patch rule fields |
| `deleteRule(ruleId, userId)` | Delete rule (userId ownership guard) |

**Bulk insert chunking:**
```typescript
for (let i = 0; i < rows.length; i += 1000) {
  await db.insert(glReviewTransactions).values(rows.slice(i, i + 1000));
}
```

---

## 12. Frontend Architecture

### 12.1 Pages

All pages use React Router v6 nested routes under `GLReviewLayout`.

#### `GLReviewUpload.tsx` — Upload & Run History
- Drag-and-drop file zone (`.csv`, `.xlsx`)
- Config panel: AI provider, explanations toggle, notify email, enableMl, fiscalYearStartMonth
- Upload progress indicator
- Run history table: status, file name, row count, flagged count, created date, delete action

#### `GLReviewDashboard.tsx` — Executive Dashboard
- Run status banner (progress while running, phase name)
- 4 KPI cards: Total Transactions, Flagged, Critical/High, Avg Risk Score
- Risk distribution pie chart (`Recharts PieChart`)
- Monthly trend area chart (`Recharts AreaChart`)
- Top 8 flagged transactions table
- AI Executive Summary (markdown rendered via `GlMarkdown.tsx`)
- Auto-refresh polling while `status ∈ {running, pending}`

#### `GLReviewTransactions.tsx` — Transaction Explorer
- Paginated table (50 rows default)
- Filter bar: tier chips, account dropdown, source type, posting type, flag, search, date range
- Per-row: risk badge, amount, account, entity, description, date, flag chips
- Expandable row: per-detector evidence breakdown + AI explanation callout + feedback buttons
- CSV export

#### `GLReviewChat.tsx` — AI Chat
- Multi-turn chat interface; history maintained in React state (client-side only)
- Suggested starter questions
- AI model selector (Gemini Pro, GPT-4o, Qwen)
- Disabled while run is not `completed`

#### `GLReviewAnomaly.tsx` — Anomaly Deep-Dives
- Benford's Law chart (digit distribution per account)
- Monetary flows rarity heatmap
- Entity risk scatter plot
- Amount distribution histogram
- Detector breakdown donut

#### `GLReviewComparison.tsx` — Period-over-Period Analysis
- Period selector (quarters/months per fiscal year start)
- Metrics delta cards
- Volume trend overlay chart
- Risk shift stacked bar
- Account/entity activity changes table
- Change items timeline

#### `GLReviewSettings.tsx` — Configuration
- AI provider selector + explanations toggle
- Risk tier reference card (score ranges + colors)
- Detector weight table (read-only reference)
- Fiscal year start month selector
- Rule CRUD: create/edit/delete/toggle active via `RuleEditorDialog`

---

### 12.2 Components

| Component | Purpose |
|---|---|
| `GLReviewLayout.tsx` | Persistent shell: sidebar nav, context provider, `<Outlet>` |
| `GLReviewContext.tsx` | State: `currentRunId`, `setCurrentRunId`, `uploadFile`, `company`, `fiscalPeriod` |
| `RiskBadge.tsx` | Color-coded pill: Critical=red, High=orange, Medium=yellow, Low=blue, Normal=grey |
| `riskColors.ts` | CSS vars + hex colors + client-side `scoreToTier(score: number): RiskTier` |
| `AiInterpretationPanel.tsx` | Callout with sparkles icon showing `aiExplanation` + suggested action |
| `RuleEditorDialog.tsx` | Modal: type selector + dynamic condition builder per rule type |
| `GlMarkdown.tsx` | Lightweight markdown renderer (headings, bold, bullets) — for executive summary |
| `MetricCard.tsx` | KPI card: `{ label, value, sublabel, trend? }` |
| `UploadHistory.tsx` | Paginated run history table (polls every 3s for pending/running runs) |
| `RunStatusBanner.tsx` | Top banner: spinner + phase name while running; success/error badges |

---

### 12.3 Client API Library

**File:** `src/lib/glReviewApi.ts`

All HTTP calls via `fetch` with session cookies. Responses passed through `deepCamel()` (snake_case → camelCase recursive conversion).

```typescript
uploadGlFile(file, options)                          → { runId, status, rowCount }
getRun(runId)                                        → AnalysisRun
listRuns(page, pageSize)                             → { runs, total }
getRunAnalytics(runId, periodOpts?)                  → GlAnalytics
getTransactions(runId, filters, page, size)          → TransactionPage
getFilterOptions(runId)                              → FilterOptions
downloadTransactionsCsv(runId, filters)              → Blob
submitFeedback(runId, rowId, verdict, notes?)        → void
glReviewChat(runId, message, aiModel, history)       → string
listRules()                                          → GlReviewRule[]
createRule(input)                                    → GlReviewRule
updateRule(ruleId, patch)                            → GlReviewRule
deleteRule(ruleId)                                   → void
```

---

## 13. Type Contracts

### Frontend Types (`src/types/glReview.ts`)

```typescript
type RiskTier = 'normal' | 'low' | 'medium' | 'high' | 'critical';
type FeedbackVerdict = 'true_positive' | 'false_positive' | 'needs_review';
type AiProvider = 'GEMINI' | 'OPENAI' | 'QWEN';

interface GLTransaction {
  id: number;
  runId: number;
  rowIndex: number;
  transactionId: string;
  transactionDate: string;        // ISO date
  createdDate: string;
  accountId: string;
  accountName: string;
  accountType: string;
  postingType: 'Debit' | 'Credit';
  amount: number;
  currency: string;
  entityName: string;
  description: string;
  sourceType: string;
  createdBy: string;
  compositeRiskScore: number;     // [0.00–1.00]
  riskTier: RiskTier;
  anomalyFlags: string[];
  detectorScores: Record<string, DetectorEvidence>;
  aiExplanation?: string;
  suggestedAction?: string;
  feedback?: TransactionFeedback;
}

interface DetectorEvidence {
  score: number;
  flag: string;
  evidence: string;
}

interface TransactionFeedback {
  id: number;
  verdict: FeedbackVerdict;
  notes?: string;
  createdAt: string;
}

interface AnalysisRun {
  id: number;
  status: 'pending' | 'running' | 'completed' | 'failed';
  fileName: string;
  rowCount: number;
  flaggedCount: number;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  aiProvider?: AiProvider;
  aiExplanationsEnabled: boolean;
  aiExecutiveSummary?: string;
  notifyEmail?: string;
  enableMl: boolean;
  fiscalYearStartMonth?: number;
}

interface GlReviewRule {
  id: number;
  name: string;
  ruleType: string;
  conditionJson: Record<string, unknown>;
  severity: RiskTier;
  action: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
```

### Backend Types (`server/lib/glReview/types.ts`)

```typescript
interface DetectorInput {
  rowIndex: number;
  transactionId: string;
  transactionDate: Date;
  createdDate: Date;
  accountId: string;
  accountName: string;
  accountType: string;
  postingType: 'Debit' | 'Credit';
  amount: number;
  currency: string;
  entityName: string;
  description: string;
  sourceType: string;
  createdBy: string;
  features?: GlReviewFeature;   // Attached after feature extraction
}

interface DetectorSignal {
  score: number;                // [0, 1]
  flag: string;
  evidence: string;
  tierFloor?: RiskTier;
}

interface Detector {
  name: string;
  weight: number;
  run(rows: DetectorInput[]): Map<number, DetectorSignal>;
}

interface DetectorResult {
  transactionId: number;
  compositeRiskScore: number;   // [0.00–1.00]
  riskTier: RiskTier;
  anomalyFlags: string[];
  zscoreValue?: number;
  detectorScores: Record<string, DetectorSignal>;
}
```

---

## 14. Database Migration History

| Migration File | Schema Changes |
|---|---|
| `0057_glamorous_lilith.sql` | Initial GL schema: `gl_review_runs`, `gl_review_transactions`, `gl_review_results` |
| `0058_gl_review_ai_model.sql` | Add `aiProvider`, `aiExplanationsEnabled`, `aiExecutiveSummary` to runs |
| `0059_gl_review_detector_scores.sql` | Add `detectorScores` JSONB to results |
| `0060_gl_review_rules.sql` | Create `gl_review_rules` table + indexes |
| `0061_gl_review_feedback.sql` | Create `gl_review_feedback` (append-only verdicts) |
| `0062_gl_review_runs_notify.sql` | Add `notifyEmail`, `notifiedAt`, `notifyStatus` to runs |
| `0063_gl_review_enable_ml.sql` | Add `enableMl` boolean to runs |
| `0064_gl_review_features.sql` | Create `gl_review_features` table (ML feature matrix) |
| `0065_gl_review_fiscal_year.sql` | Add `fiscalYearStartMonth` to runs |

**Total migrations:** 9 (0057–0065)

---

## 15. End-to-End Data Flow

```
Step 1 — Upload
────────────────
User → GLReviewUpload.tsx → glReviewApi.uploadGlFile()
  → POST /api/gl-review/upload
  → normalizeGlFile(buffer) → NormalizedGlRow[]
  → db.insert(gl_review_runs) → runId
  → insertTransactions(runId, rows) [chunked 1000/statement]
  → glReviewQueue.add({ runId, userId })
  → Response: { runId, status: 'pending', rowCount }

Step 2 — Async Analysis
────────────────────────
markRunRunning(runId)
extractFeatures(rows) → writeFeatures()

Run all detectors:
  [zscore, duplicate, threshold, backdating, roundNumber,
   periodEnd, benford, weekend, dcImbalance, suspenseAccount,
   negativeReversal, unusualUser, holidayPosting, taxAnomaly,
   missingDescription, rulesDetector?]
  + [isolationForest, dbscan, apriori]  ← if enableMl=true

For each row:
  composite = noisyOR(signals)
  tier = max(scoreToTier(composite), maxTierFloor)
  → DetectorResult{ transactionId, compositeRiskScore, riskTier, anomalyFlags, detectorScores }

writeResults(results)

AI Tier (best-effort, try/catch):
  topRows = getTopResultsForExplanation(runId, limit=200)
  vendorProfiles = buildVendorPatternProfiles(topRows)
  → Vendor story path OR flag-only path
  updateAiExplanations()
  generateExecutiveSummary() → updateAiExecutiveSummary()

markRunCompleted(runId, flaggedCount)
glReviewNotifyQueue.add(runId)

Step 3 — Notification
──────────────────────
if notifyEmail AND criticalHighCount > 0:
  sendEmail(notifyEmail, summaryHtml)
  updateNotifyStatus('sent')

Step 4 — Frontend Polls
────────────────────────
GET /runs/:runId every 3s while status ∈ {pending, running}
On completed → fetch analytics → render GLReviewDashboard

Step 5 — Auditor Interaction
──────────────────────────────
GLReviewTransactions: GET /transactions?riskTier=critical
Auditor reviews row → "True Positive"
  → POST /transactions/:rowId/feedback
  → appendFeedback(runId, rowId, userId, 'true_positive', notes)
  → Row shows feedback badge in UI

Step 6 — AI Chat
──────────────────
User types in GLReviewChat
  → POST /runs/:runId/chat { message, history, aiModel }
  → analyticsContext injected into system prompt
  → Single-turn LLM call → plain text answer
  → History maintained in React state (client-side)
```

---

## 16. Key Design Decisions & Rationale

### 16.1 Weighted Noisy-OR vs Simple Addition

**Decision:** `1 − ∏(1 − wᵢsᵢ)` instead of `Σ(wᵢsᵢ)`

**Rationale:** Additive scores can exceed 1.0, requiring arbitrary normalization. Noisy-OR models "at least one cause" probability — statistically appropriate for independent anomaly evidence sources. High-weight detectors still dominate, and multiple weak signals compound naturally.

### 16.2 Tier Floors for Business Rules

**Decision:** Rules set `tierFloor`; final tier = `max(scoreToTier(composite), tierFloor)`.

**Rationale:** Business rules encode hard compliance requirements. A restricted-vendor transaction must be `critical` regardless of how small the amount is statistically. The floor mechanism separates deterministic policy from probabilistic detection.

### 16.3 Best-Effort AI Block

**Decision:** Entire AI narration wrapped in try/catch; never fails the run.

**Rationale:** AI providers have rate limits, occasional downtime, and content refusals. The core value — risk scores and detector evidence — must always be available. AI explanations enhance but do not replace the statistical output.

### 16.4 Append-Only Feedback

**Decision:** `gl_review_feedback` is insert-only; current verdict = latest by `createdAt`.

**Rationale:** Audit trail immutability (SOX/PCAOB). Auditors can revise verdicts without destroying history. Enables future ML training on verdict sequences. Simplifies multi-auditor conflict resolution.

### 16.5 Per-Account Z-Score Baseline

**Decision:** Account peer group when ≥10 same-account transactions exist; global fallback otherwise.

**Rationale:** A $5M entry to "Cash" is normal; a $5M entry to "Petty Cash" is anomalous. Global Z-Score misses intra-account outliers. The 10-row minimum prevents false positives from tiny sample sizes.

### 16.6 Chunked Bulk Insert at 1000 Rows

**Decision:** Cap `INSERT` statements at 1000 rows per batch.

**Rationale:** PostgreSQL bind parameter limit is 65,535. With ~15 columns per row: 65,535 / 15 ≈ 4,369 rows max. Conservative cap of 1,000 provides buffer for future schema additions.

### 16.7 Feature Extraction as a Separate Persisted Pass

**Decision:** Compute ML features once, persist to `gl_review_features`, reuse across all Tier-2 detectors.

**Rationale:** Features like `accountMean`, `accountStddev`, `zscoreAmount` require a full dataset scan. Multiple detectors need the same features. Persisting enables Apriori cross-run analysis (queries prior run feature rows). Single-pass is more efficient than each detector recomputing independently.

### 16.8 Vendor Story vs Flag-Only AI Paths

**Decision:** Two prompt paths depending on whether a temporal vendor profile can be constructed.

**Rationale:** Temporal narrative ("amount tripled vs last 6 months") is far more actionable than generic flag descriptions. Not all transactions have sufficient vendor history. Separate paths allow optimized prompt design. Batching (8 vendors / 10 rows per call) reduces LLM API costs.

---

## 17. Complete File Index

### Backend

| File | Purpose |
|---|---|
| `server/lib/glReview/registry.ts` | Orchestration: builds detector list, runs all, calls fusion |
| `server/lib/glReview/aggregate.ts` | Noisy-OR fusion, `scoreToTier()`, tier floor logic |
| `server/lib/glReview/types.ts` | `Detector`, `DetectorInput`, `DetectorSignal`, `DetectorResult` |
| `server/lib/glReview/rules.ts` | Rule condition schemas (Zod), `makeRuleDetector()` |
| `server/lib/glReview/detectors/zscore.ts` | Per-account Z-Score outlier |
| `server/lib/glReview/detectors/duplicate.ts` | Exact-key hash duplicate |
| `server/lib/glReview/detectors/threshold.ts` | Approval threshold structuring |
| `server/lib/glReview/detectors/backdating.ts` | Post-period backdating |
| `server/lib/glReview/detectors/roundNumber.ts` | Round amount |
| `server/lib/glReview/detectors/periodEnd.ts` | Period-end posting |
| `server/lib/glReview/detectors/benford.ts` | Benford's Law first-digit |
| `server/lib/glReview/detectors/weekend.ts` | Weekend posting |
| `server/lib/glReview/detectors/dcImbalance.ts` | Debit/credit imbalance |
| `server/lib/glReview/detectors/isolationForest.ts` | Isolation Forest ML |
| `server/lib/glReview/detectors/dbscan.ts` | DBSCAN clustering ML |
| `server/lib/glReview/detectors/apriori.ts` | Apriori cross-run ML |
| `server/lib/glReview/features/features.ts` | Feature extraction entry point |
| `server/lib/glReview/features/primitives.ts` | Reusable primitive functions |
| `server/lib/glReviewNormalize.ts` | CSV/XLSX parsing + normalization |
| `server/lib/glReviewAnalytics.ts` | Analytics computation engine |
| `server/workers/glReviewWorker.ts` | Async analysis pipeline (Bull job) |
| `server/workers/glReviewNotifyWorker.ts` | Email notification (Bull job) |
| `server/routes/gl-review/index.ts` | Route registration + auth middleware |
| `server/routes/gl-review/upload.ts` | POST /upload |
| `server/routes/gl-review/runs.ts` | GET /runs, GET /runs/:id, DELETE |
| `server/routes/gl-review/transactions.ts` | GET /transactions, GET /filter-options |
| `server/routes/gl-review/chat.ts` | POST /chat |
| `server/routes/gl-review/export.ts` | GET /export (CSV) |
| `server/routes/gl-review/feedback.ts` | POST /feedback |
| `server/routes/gl-review/rules.ts` | GET/POST/PATCH/DELETE /rules |
| `server/storage/gl-review.ts` | Database access layer |
| `server/schemas/glReviewSchemas.ts` | Zod validation schemas |

### Frontend

| File | Purpose |
|---|---|
| `src/pages/GLReviewUpload.tsx` | Upload page + run history |
| `src/pages/GLReviewDashboard.tsx` | Executive dashboard |
| `src/pages/GLReviewTransactions.tsx` | Paginated explorer + feedback |
| `src/pages/GLReviewChat.tsx` | Multi-turn AI chat (client-side history) |
| `src/pages/GLReviewAnomaly.tsx` | Anomaly deep-dive charts |
| `src/pages/GLReviewComparison.tsx` | Period-over-period comparison |
| `src/pages/GLReviewSettings.tsx` | Rules + AI config + tier reference |
| `src/components/glReview/GLReviewLayout.tsx` | Persistent shell |
| `src/components/glReview/GLReviewContext.tsx` | React context |
| `src/components/glReview/RiskBadge.tsx` | Color-coded tier badge |
| `src/components/glReview/riskColors.ts` | CSS vars + `scoreToTier()` |
| `src/components/glReview/AiInterpretationPanel.tsx` | AI explanation callout |
| `src/components/glReview/RuleEditorDialog.tsx` | Rule create/edit modal |
| `src/components/glReview/GlMarkdown.tsx` | Markdown renderer |
| `src/components/glReview/MetricCard.tsx` | KPI card |
| `src/components/glReview/UploadHistory.tsx` | Run history table |
| `src/components/glReview/RunStatusBanner.tsx` | Progress/error banner |
| `src/lib/glReviewApi.ts` | HTTP client (all GL Review endpoints) |
| `src/types/glReview.ts` | Frontend TypeScript types |
| `src/data/glReviewMock.ts` | Mock data (initial UI development) |

### Database

| File | Purpose |
|---|---|
| `shared/schema/tables-gl-review.ts` | Drizzle ORM definitions (6 tables) |
| `migrations/0057_glamorous_lilith.sql` | Initial GL schema |
| `migrations/0058_gl_review_ai_model.sql` | AI columns on runs |
| `migrations/0059_gl_review_detector_scores.sql` | detectorScores JSONB |
| `migrations/0060_gl_review_rules.sql` | Business rules table |
| `migrations/0061_gl_review_feedback.sql` | Auditor feedback table |
| `migrations/0062_gl_review_runs_notify.sql` | Notification columns |
| `migrations/0063_gl_review_enable_ml.sql` | ML enable flag |
| `migrations/0064_gl_review_features.sql` | ML feature matrix table |
| `migrations/0065_gl_review_fiscal_year.sql` | Fiscal year start month |

---

*End of Report — Initial Branch*

**Totals:**
- Tier-1 detectors: **15**
- Tier-2 ML detectors: **3** (Isolation Forest, DBSCAN, Apriori)
- Tier-3 rule engine: **1** closure detector (unlimited rule instances)
- Tier-4 AI: best-effort, all flagged rows, no cap
- Database tables: **6** (`gl_review_*` prefix)
- API routes: **14**
- Frontend pages: **7**
- Migrations: **9** (0057–0065)
- Score range: **0.00–1.00** (float)
- Risk tier thresholds: **0.85 / 0.65 / 0.40 / 0.15** (fixed)
- Default AI providers: **GEMINI, OPENAI, QWEN**