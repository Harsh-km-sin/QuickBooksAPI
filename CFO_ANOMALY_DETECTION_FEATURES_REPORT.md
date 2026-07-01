# CFO Anomaly Detection App - Feature Report
**Generated:** June 12, 2026  
**Project:** QuickBooks API Platform

---

## Executive Summary

Your QuickBooks API project has a **solid foundation for a CFO-grade anomaly detection app**. It combines real-time financial data synchronization from QuickBooks Online with a sophisticated data warehouse, intelligent anomaly rules engine, and an interactive dashboard. The system is designed for multi-company, multi-user SaaS operations.

**Current State:** You have ~70% of the core infrastructure for an anomaly detection platform. Key gaps exist in advanced rule sophistication and ML-based detection.

---

## 1. CORE FINANCIAL DATA INFRASTRUCTURE ✓ STRONG

### What You Have
- **Real-time QBO Integration**: OAuth 2.0 connection to QuickBooks Online with automatic token refresh
- **Multi-company Support**: Each user can connect multiple QBO companies independently
- **Complete Entity Sync**: Customers, Vendors, Products, Chart of Accounts, Invoices, Bills, Journal Entries
- **Sync Orchestration**: Service Bus-driven async full-sync pipeline with staged entity processing
- **Data Warehouse**: Purpose-built dimensional & fact tables for financial analysis

### Key Assets
✓ Dim tables: DimCustomer, DimVendor, DimAccount (GL classification)  
✓ Fact tables: FactRevenue, FactExpenses, FactVendorSpend  
✓ Analytics tables: KPI snapshots, Close issues, Forecast scenarios  
✓ Incremental sync capability (tracks changes since last sync)  

### Readiness for Anomaly Detection
**9/10** — You have clean, structured data flowing into a warehouse. Anomalies need good data.

---

## 2. CURRENT ANOMALY DETECTION SYSTEM ⚠️ BASIC BUT FUNCTIONAL

### Implemented Detection Rules

#### Rule 1: Vendor Spend Spike (Medium Severity)
```
Trigger: Current month spend > 1.5x previous month
Impact: Identifies unusual vendor payment patterns
Limitation: Fixed threshold, no seasonality adjustment
```
**CFO Use Case:** Catch unexpected vendor bills or duplicate payments

#### Rule 2: Large Transaction (Low Severity)
```
For Revenue: Invoices > 2x average invoice amount
For Expenses: Bills > 2x average bill amount
Impact: Flags unusual transaction sizes
Limitation: Simple statistical, doesn't account for customer/vendor relationships
```
**CFO Use Case:** Detect potential data entry errors or unusual deals

#### Rule 3: Overdue Receivables (High Severity)
```
Trigger: ≥5 invoices >90 days overdue
Impact: Cash flow risk indicator
Limitation: Only binary (yes/no), no risk scoring
```
**CFO Use Case:** Identify collection challenges and cash flow threats

### Current Architecture
```
Sync Completion → Financial Warehouse Rebuild → Anomaly Detection Engine → anomaly_events Table → Dashboard
```

### Rule Storage & Execution
- Rules defined in `AnomalyDetectionService` (code-based)
- Runs post-sync, computes facts from warehouse tables
- Results persist to `anomaly_events` table with: Type, Severity, Details, DetectedAt
- Exposed via `/api/analytics/anomalies` endpoint

### Readiness for Anomaly Detection
**5/10** — Rules are basic. Good for MVP, but lacks sophistication needed for enterprise CFO.

---

## 3. CFO-GRADE FINANCIAL ANALYTICS ✓ COMPREHENSIVE

### Cash & Liquidity Intelligence
✓ **Cash Runway Analysis** (`/api/analytics/cash-runway`)
- Current cash position (sum of bank/cash GL accounts)
- Monthly burn rate calculation
- Expected runway in months
- Conservative methodology

✓ **Vendor Spend Intelligence** (`/api/analytics/vendor-spend`)
- Top vendors by spend (configurable period & limits)
- Spend summaries and trends
- Period-based filtering

✓ **Receivables & Payables**
- Overdue tracking (by aging bucket)
- Bill vs Invoice status
- Collection and payment forecasting potential

### Profitability & Performance
✓ **Customer Profitability Ranking** (`/api/analytics/customer-profitability`)
- Revenue, COGS, Gross Margin, Margin %
- Ranked by profitability (descending)
- Configurable date range + top-N filtering

✓ **Revenue vs Expenses Trend** (`/api/analytics/revenue-expenses`)
- Monthly P&L trends (12+ months)
- Perfect for forecasting baseline

✓ **Consolidated Financial View** (`/api/analytics/consolidated-pnl`)
- Parent/child entity consolidation support
- Multi-company rollup capability

### Dashboard Integration
✓ **Single Dashboard Endpoint** (`/api/dashboard/summary`)
- Returns 13+ analytics in one call via parallel Task.WhenAll()
- Includes: metrics, cash runway, vendors, profitability, revenue/expenses, anomalies, KPIs, close issues
- Performance optimized

### Readiness for Anomaly Detection
**8/10** — Excellent analytics foundation. Anomaly detection needs good context data (which you have).

---

## 4. DATA QUALITY & CLOSE MANAGEMENT ✓ PRESENT

### Close Issues Tracking
- `close_issues` table: Tracks data quality findings
- `CloseIssuesFunction`: Daily detection of month-end data quality issues
- Issues tagged by: Type, Severity, Details, ResolvedAt timestamp
- Integrated into dashboard

### Examples of Trackable Issues
- Missing invoice details
- Unreconciled transactions
- Duplicate entries
- GL account mismatches
- Balance sheet reconciliation gaps

### Readiness for Anomaly Detection
**7/10** — Close issues are tracking-based, not predictive. Good complement to anomalies.

---

## 5. HISTORICAL KPI & TREND ANALYSIS ✓ FOUNDATION

### KPI Infrastructure
✓ `KpiSnapshot` table: Daily calculations of key metrics (cash, revenue, expenses, margin %)  
✓ `KpiSnapshotFunction`: Automated daily capture  
✓ Historical trend data available for all major metrics  

### Use Cases
- Detect KPI degradation trends
- Anomaly severity: Is spike worse than historical patterns?
- Forecasting: Use KPI history for baseline models

### Readiness for Anomaly Detection
**8/10** — Rich historical data enables pattern-based and predictive anomalies.

---

## 6. FORECASTING & SCENARIO PLANNING ✓ INTEGRATED

### Forecast Tables
- `forecast_scenarios`: Scenario definitions (Name, HorizonMonths, AssumptionsJson)
- `forecast_results`: Projected financials (Revenue, Expenses, NetIncome, CashBalance, RunwayMonths)

### Integration with Anomalies
- Compare actual vs forecast: Major deviations = anomalies
- Scenario-based anomaly thresholds (conservative vs aggressive)

### Readiness for Anomaly Detection
**6/10** — Forecast tables exist but seem lightly integrated. Opportunity for forecast-variance anomalies.

---

## 7. FRONTEND CAPABILITIES ✓ SOLID

### Pages & Features
✓ Dashboard: Overview + metrics + anomalies  
✓ Anomalies page: List of detected issues with severity badges  
✓ Close Assistant: Data quality tracking for month-end  
✓ CFO Assistant: Natural language Q&A with rule-based intent matching + optional LLM  
✓ Connected Companies: Multi-company selector  
✓ Entity management: CRUD for customers, vendors, products  

### Analytics Visualizations
✓ Cash runway widget  
✓ Top vendors chart  
✓ Customer profitability ranking  
✓ Revenue vs Expenses trend  
✓ KPI sparklines & history  

### Readiness for Anomaly Detection
**7/10** — Dashboard exists, but anomalies are basic list view. Opportunity for richer viz & drill-down.

---

## 8. AI & INTELLIGENT ASSISTANCE ✓ STARTED

### CFO Assistant
- Natural language input: "What's our cash position?"
- Rule-based intent matching (cash, revenue, vendors, profitability)
- Optional Azure OpenAI summarization
- Citation support (shows which API calls provided data)

### Current Intents
- Cash-related questions
- Revenue questions
- Vendor/spend questions
- Profitability questions

### Readiness for Anomaly Detection
**5/10** — Assistant is Q&A focused. Not yet integrated with anomaly explanation or severity.

---

## 9. DEPLOYMENT & SCALABILITY ✓ ENTERPRISE-READY

### Azure Architecture
✓ App Service: ASP.NET Core API  
✓ Function App: SyncWorker (Azure Functions)  
✓ Azure SQL Database: Central store  
✓ Service Bus: Decoupled async jobs  
✓ Application Insights: Monitoring (optional)  
✓ Azure OpenAI: Optional LLM (CFO Assistant)  

### Scalability Features
✓ Service Bus decoupling  
✓ Per-company RealmId isolation  
✓ Incremental sync capability  
✓ Entity-level parallelization ready  

### Readiness for Anomaly Detection
**8/10** — Excellent infrastructure. Scales with companies & historical data load.

---

## 10. FEATURE MATURITY MATRIX

| Feature Category | Implementation | Gaps | Priority |
|------------------|-----------------|------|----------|
| **Data Sync** | ✓ Complete | None | N/A |
| **Financial Warehouse** | ✓ Complete | None | N/A |
| **Anomaly Rules** | ⚠️ Basic (3 rules) | Advanced rules, ML models | HIGH |
| **Anomaly UI** | ⚠️ Basic list | Drill-down, root cause, alerts | HIGH |
| **KPI Tracking** | ✓ Complete | Forecasting integration | MEDIUM |
| **Close Management** | ⚠️ Basic tracking | Automation, ML patterns | MEDIUM |
| **Forecasting** | ✓ Tables created | Integration with anomalies | MEDIUM |
| **CFO Assistant** | ⚠️ Basic Q&A | Anomaly explanation, reasoning | MEDIUM |
| **Alerting** | ✗ Not present | Real-time alerts, webhooks, email | HIGH |
| **Root Cause Analysis** | ✗ Not present | ML clustering, relationships | HIGH |
| **Benchmarking** | ✗ Not present | Industry/peer comparison | LOW |
| **Custom Rules** | ✗ Not present | User-defined rules engine | MEDIUM |

---

## 11. STRENGTHS FOR ANOMALY DETECTION

### ✓ What You're Doing Well

1. **Clean Data Foundation**
   - Structured warehouse with dimensional modeling
   - Real-time sync from authoritative source (QBO)
   - No data quality issues at ingestion

2. **Multi-dimensional Context**
   - Customer, Vendor, GL Account dimensions enable root cause analysis
   - Can slice anomalies by: customer, vendor, account, time period

3. **Historical Trend Data**
   - KPI snapshots capture daily metrics
   - Enables statistical anomaly detection (z-score, seasonal adjustment)

4. **Scalable Architecture**
   - Service Bus decoupling means anomaly rules can scale independently
   - Azure Functions can be auto-scaled during heavy analysis periods

5. **Business Logic Foundation**
   - Cash runway, profitability, spend analytics already exist
   - Easy to add anomaly variants on these existing calculations

6. **Multi-company Support**
   - Tenancy is built-in (RealmId isolation)
   - Each company can have custom anomaly rules/thresholds

---

## 12. GAPS & IMPROVEMENT OPPORTUNITIES

### 🔴 Critical Gaps

#### 1. **Rule Sophistication**
**Current State:** 3 fixed-threshold rules in code  
**Gap:** No statistical, seasonal, or ML-based detection  
**Impact:** High false positives, misses subtle anomalies

**Improvements:**
- Statistical rules: Z-score (detect outliers by standard deviation)
- Seasonal adjustment: Account for Q4 spending spikes
- Trend rules: Detect sustained degradation (not just 1-month jumps)
- Forecasting variance: Compare actuals vs forecast

#### 2. **Real-time Alerting**
**Current State:** Anomalies computed post-sync, must visit dashboard to see  
**Gap:** No proactive notification system  
**Impact:** CFO may miss time-critical anomalies

**Improvements:**
- Email alerts for High severity anomalies
- Slack/Teams webhooks for urgent issues
- Real-time dashboard push (WebSocket or polling)
- Alert scheduling (daily digest vs real-time)

#### 3. **Root Cause Analysis**
**Current State:** Anomalies list type + details, no deeper investigation  
**Gap:** CFO can't easily drill down to understand "why"  
**Impact:** Requires manual investigation, slows decision-making

**Improvements:**
- Related transactions view: Show invoices/bills contributing to spike
- Dimensional drill-down: Anomaly by customer, vendor, GL account
- Peer comparison: Show how this vendor compares to others
- Forecast variance: How much worse than expected?

#### 4. **Custom Rule Engine**
**Current State:** Rules hardcoded in C#  
**Gap:** CFO can't define their own rules or adjust thresholds  
**Impact:** Inflexible, requires dev change for any tuning

**Improvements:**
- Rules table in DB: Type, Threshold, Enabled, CreatedBy
- Rule builder UI: Drag-and-drop or simple form
- Per-company rule customization
- A/B testing framework for rule tuning

#### 5. **ML-Based Anomaly Detection**
**Current State:** Statistical rules only  
**Gap:** No learned patterns, seasonal adjustments, or clustering  
**Impact:** Limited to human-defined rule thresholds

**Improvements:**
- Isolation Forest / One-Class SVM for unsupervised anomalies
- Seasonal ARIMA for forecasting-based detection
- Clustering to group similar vendors/customers
- Time-series forecasting (Prophet, LSTM)

---

### 🟡 Medium Gaps

#### 6. **Anomaly Severity & Scoring**
**Current State:** 3 severity levels (Low/Medium/High)  
**Gap:** No risk scoring or impact quantification  
**Impact:** CFO doesn't know which anomaly to act on first

**Improvements:**
- Financial impact score ($ amount at risk)
- Probability scoring (how likely is this a real issue?)
- Priority ranking: Impact × Probability × Timeliness

#### 7. **Anomaly Context & Explanation**
**Current State:** Type + Details, no natural language explanation  
**Gap:** CFO needs to understand what the anomaly means in context  
**Impact:** Requires manual interpretation

**Improvements:**
- Auto-generated summary: "Vendor ABC had a 60% spend increase this month"
- Comparison context: "This is 3x higher than their average spend"
- Suggested actions: "Review invoice line items" or "Contact vendor"
- LLM integration with CFO Assistant for deeper analysis

#### 8. **Forecasting Integration**
**Current State:** Forecast tables exist but not integrated with anomalies  
**Gap:** No variance detection between forecast and actual  
**Impact:** Can't detect if actuals diverge significantly from plan

**Improvements:**
- Post-sync: Compare actuals vs latest forecast
- Compute variance %: How far off plan are we?
- Forecast variance anomalies: Trigger if > 10% deviation
- Forecast scenario: Which scenario best matches actuals?

#### 9. **Consolidation & Multi-Company Anomalies**
**Current State:** Per-company detection only  
**Gap:** Can't detect anomalies at consolidated level  
**Impact:** Misses roll-up issues (e.g., duplicate expense across companies)

**Improvements:**
- Consolidated anomaly detection (parent-level)
- Drill-down to company source
- Cross-company comparison: Is this vendor expensive compared to sister companies?

---

### 🟢 Nice-to-Have Gaps

#### 10. **Benchmarking & Peer Comparison**
**Current State:** Metrics are standalone  
**Gap:** No comparison to industry standards or competitors  
**Impact:** Can't assess if metrics are healthy relative to peers

**Improvements:**
- Industry benchmarking data (SIC code benchmarks)
- Peer company comparison (anonymized, crowdsourced)
- Ratio analysis: Metrics vs peers

#### 11. **Anomaly History & Trend**
**Current State:** Anomalies deleted or archived  
**Gap:** Can't see "which anomalies resolved" or "recurring issues"  
**Impact:** No learning from past anomalies

**Improvements:**
- Anomaly resolution tracking: Status, ResolvedBy, ResolvedAt, RootCause
- Recurring anomaly detection: Same type > 3x in 6 months
- Anomaly trend analysis: Are anomalies increasing or decreasing?

#### 12. **Audit & Compliance**
**Current State:** No anomaly audit trail  
**Gap:** Can't trace who investigated or acted on anomalies  
**Impact:** SOX/compliance risk

**Improvements:**
- User actions on anomalies (Acknowledged, Investigated, Dismissed)
- Audit log: Who did what, when
- Compliance report: Anomalies by category, resolution rate

---

## 13. RECOMMENDED ROADMAP FOR ANOMALY DETECTION

### Phase 1: Foundation (2-3 sprints) — From Basic to Solid
**Goal:** Make anomaly detection production-ready with better rules

**Tasks:**
1. Add statistical rules (Z-score detection)
2. Implement seasonal adjustment for annual patterns
3. Build anomaly drill-down UI (show related transactions)
4. Add anomaly resolution workflow (status tracking)
5. Email alerting for High severity anomalies

**Outcome:** CFO can investigate anomalies quickly with actionable context

---

### Phase 2: Intelligence (2-3 sprints) — Add ML & Custom Rules
**Goal:** Smarter detection + flexibility for CFO-defined rules

**Tasks:**
1. Build rule engine (UI + database-backed rules)
2. Add forecasting variance anomalies
3. ML: Implement Isolation Forest for unsupervised detection
4. CFO Assistant: Explain anomalies in natural language
5. Consolidation-level anomaly detection

**Outcome:** Fewer false positives, faster root cause analysis, personalized rules

---

### Phase 3: Mastery (2-3 sprints) — Advanced Analysis
**Goal:** Deep insights, benchmarking, predictive anomalies

**Tasks:**
1. Time-series forecasting (Prophet/LSTM for next-month prediction)
2. Anomaly severity scoring (Financial Impact × Probability)
3. Cross-company benchmarking
4. Recurring anomaly detection & trending
5. Audit trail & compliance reporting

**Outcome:** CFO has predictive foresight, can benchmark performance, compliance-ready

---

### Phase 4: Scale (Ongoing) — Optimization
**Goal:** Performance, customization, ecosystem

**Tasks:**
1. Real-time detection (sub-minute latency)
2. Webhook integrations (Slack, Teams, custom)
3. Mobile app for anomaly review
4. Multi-tenant rule libraries (share best practices)
5. Anomaly prediction (forecast future anomalies)

**Outcome:** Enterprise-scale CFO platform with best-in-class anomaly detection

---

## 14. TECHNICAL DEBT & ARCHITECTURE NOTES

### Current Strengths
- ✓ Clean separation of concerns (Services, Repositories, API)
- ✓ Async patterns via Service Bus
- ✓ Dependency injection for testability
- ✓ Warehouse for reporting isolation from operational DB

### Areas for Attention
- Rules are in code (not scalable for custom rules)
- No alerting infrastructure (would need redesign)
- Forecasting tables exist but under-utilized
- CFO Assistant not integrated with anomaly context
- Severity is fixed (not scored by impact)

---

## 15. COMPETITIVE POSITIONING

Your QuickBooks anomaly detection app competes with:
- **NetSuite OpenAI Advisor** — Natural language Q&A (you have this partially)
- **Anaplan** — Forecasting + variance (you have foundations)
- **Alteryx** — Data quality (you have close issues tracking)
- **Domo** — BI + alerts (you have BI, alerts are a gap)

### Your Differentiation Opportunity
✓ Real-time QBO sync (Domo, Anaplan require manual integration)  
✓ Multi-company support (out-of-box vs custom config elsewhere)  
✓ Custom rule engine (vs fixed rules in competitors)  
✓ Deep GL accounting context (vs high-level metrics)  
✓ SMB/Mid-market focus (competitors are enterprise-first)  

**To Win:**
1. Make rule customization simple (UI, not code)
2. Add ML detection (differentiate from static rules)
3. Speed of detection (sub-minute latency)
4. Integrate with QBO bookkeeper ecosystem

---

## 16. QUICK-WIN IMPROVEMENTS (1-2 sprints)

If you want fast wins before diving into roadmap:

1. **Add Z-score Rule** (2-3 days)
   - Detect Vendor Spend outliers by standard deviation
   - More robust than fixed 1.5x threshold

2. **Drill-down UI** (3-4 days)
   - Click anomaly → show related invoices/bills
   - Group by vendor/customer/GL account

3. **Email Alerts** (2-3 days)
   - SendGrid/Azure Mail for High severity anomalies
   - Daily digest option

4. **Anomaly Resolution Workflow** (2-3 days)
   - Status: Acknowledged, Investigating, Resolved
   - Add comment/notes field

5. **Seasonal Adjustment** (3-4 days)
   - Compare to same month last year (not just previous month)
   - Reduce false positives in Q4 for retailers

**Total:** ~2 weeks, immediate CFO value

---

## Conclusion

You have built a **strong financial data foundation** with real-time QBO integration, a purpose-built data warehouse, and basic anomaly detection. The architecture is scalable and enterprise-ready.

**Your anomaly detection is currently at an MVP level (3 basic rules).** To make it a **standout product**, focus on:

1. **Rule sophistication** — Statistical + ML detection
2. **Actionable insights** — Root cause drill-down + recommendations
3. **Customization** — Let CFOs define their own rules
4. **Proactive alerting** — Don't make them check the dashboard
5. **Context & explanation** — Why matters as much as what

The roadmap above is achievable in **8-12 sprints** (4-6 months) and would position you competitively in the CFO intelligence space.

---

## Appendix: Key Endpoints Reference

### Analytics Endpoints
- `GET /api/analytics/cash-runway` — Liquidity metrics
- `GET /api/analytics/customer-profitability` — Customer ranking
- `GET /api/analytics/vendor-spend/top` — Top vendors by spend
- `GET /api/analytics/vendor-spend/summary` — Spend summary
- `GET /api/analytics/revenue-expenses` — P&L trend (12+ months)
- `GET /api/analytics/consolidated-pnl` — Multi-company consolidation
- `GET /api/analytics/anomalies` — Detected anomalies

### Dashboard
- `GET /api/dashboard/summary` — All CFO metrics in one call

### Sync
- `POST /api/company/sync/full` — Trigger full company sync
- `GET /api/company/sync/status` — Sync progress

### CFO Assistant
- `POST /api/assistant/ask` — Natural language question

### Configuration
- `GET /api/kpi/history` — KPI trend data
- `POST /api/forecast/scenario` — Create forecast scenario

---

**Report prepared for:** QuickBooks API CFO Platform  
**Next Steps:** Prioritize Phase 1 quick-wins, then scope Phase 2 roadmap with team.
