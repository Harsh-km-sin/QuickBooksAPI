# AI readiness follow-up plan

Addresses three gaps called out after the Auth / Companies vertical slices:

1. **Entity traffic** — most UI/API traffic still flows through `client.ts` and `Controllers/`.
2. **Queue contracts** — `FullSyncMessage` and peers are not versioned; producer/consumer drift is possible.
3. **Architecture test** — `Features_ShouldNotDependOn_DataAccessLayer_Repos` fails while feature services import `QuickBooksAPI.DataAccessLayer.Repos` (repository **interfaces** live in that namespace today).

---

## Workstream A — Frontend API modularization (`client.ts`)

**Goal:** Same pattern as `authApi.ts` / `companyApi.ts`: one file per domain, thin `client.ts` barrel, predictable merge boundaries.

| ID | Task |
|----|------|
| A1 | Extract **customer** API object from `client.ts` → `api/customerApi.ts`; re-export from `client.ts`. |
| A2 | Extract **product** API → `api/productApi.ts`; re-export. |
| A3 | Extract **vendor** API → `api/vendorApi.ts`; re-export. |
| A4 | Extract **bill** API → `api/billApi.ts`; re-export. |
| A5 | Extract **invoice** API → `api/invoiceApi.ts`; re-export. |
| A6 | Extract **chartOfAccounts** + **journalEntry** (+ **assistant** if bundled) → `api/chartOfAccountsApi.ts`, `api/journalEntryApi.ts`, `api/assistantApi.ts` as appropriate; re-export. |
| A7 | Grep for `@/api/client` imports; point feature code at `@/api/<domain>Api` where it improves clarity; keep barrel stable for legacy imports. |
| A8 | Update `QuickBooksAPI Frontend/app/README.md` project structure once splits land. |

**Optional later:** lazy-loaded route chunks (not required for AI readiness score).

---

## Workstream B — Backend controllers → `Features/` (incremental)

**Goal:** Move HTTP entry points out of `Controllers/` into `Features/<Capability>/` without changing routes; prioritize high-churn or entity groups.

| ID | Task |
|----|------|
| B1 | Inventory `Controllers/*.cs`; group by domain (Customer, Product, Vendor, Bill, Invoice, ChartOfAccounts, JournalEntry, Analytics, …). |
| B2 | Pilot **one entity controller** (e.g. Customer) → `Features/Customers/CustomerController.cs`; delete old file; `dotnet build` / smoke. |
| B3 | Repeat per domain in small PRs (or batches), updating `QuickBooksAPI/Features/README.md` table each time. |
| B4 | Leave cross-cutting controllers (e.g. health, swagger) in `Controllers/` if preferred. |

---

## Workstream C — Versioned queue contracts

**Goal:** Explicit schema identity for Service Bus JSON so API and `SyncWorker` evolve safely.

| ID | Task |
|----|------|
| C1 | Add shared contract project or folder: e.g. `Contracts/Messages/FullSyncMessageV1.cs` with **`SchemaVersion`** (const `1`) and stable property names; include `FullSyncMessage` migration path. |
| C2 | Reference the contract from **QuickBooksAPI** (`SyncService` publish) and **SyncWorker** (deserialize); remove duplicate `DataAccessLayer.Models.FullSyncMessage` or map old → new for one release. |
| C3 | Document rollback/compatibility in `README` or `tests/docs/FULLSYNC_WORKER_SMOKE.md` (v1 only; v2 TBD). |
| C4 | Optional: validate `SchemaVersion` in worker before orchestration; dead-letter unknown versions. |

---

## Workstream D — Env / DB parity (guardrails, not full automation)

**Goal:** Reduce “works on my machine” failures for analytics and workers.

| ID | Task |
|----|------|
| D1 | Consolidate pointers: link `PHASE3_DATABASE_CHECKLIST.md`, `TRACK_B_API_SMOKE.md`, `FULLSYNC_WORKER_SMOKE.md` from root `README.md` or `AI_READINESS_IMPROVEMENT_ROADMAP.md` “Operational parity” subsection. |
| D2 | Add **optional** CI or local script: `dotnet test` against architecture + document “required tables for Track B” (already partially in checklists). |
| D3 | Optional: lightweight **health** endpoint expansion (e.g. DB connectivity + one critical table probe) behind feature flag — only if product agrees. |

---

## Workstream E — Fix `Features_ShouldNotDependOn_DataAccessLayer_Repos`

**Root cause:** Repository **interfaces** (`IForecastScenarioRepository`, `IForecastResultRepository`, `ICloseIssueRepository`, …) live under namespace `QuickBooksAPI.DataAccessLayer.Repos`, so `Features/*` services that depend on them trigger NetArch.

**Preferred fix:** Move repository interfaces to **`Application/Interfaces`** (match `IVendorReadService` style); keep implementations in `DataAccessLayer/Repos/*.cs` with `using` updated. DI registration in `Program.cs` / extension methods unchanged except namespaces.

| ID | Task |
|----|------|
| E1 | Move `IForecastScenarioRepository`, `IForecastResultRepository` to `Application/Interfaces`; adjust implementations and `ForecastService` usings. |
| E2 | Move `ICloseIssueRepository` to `Application/Interfaces`; adjust `CloseIssueRepository` + `CloseIssueService`. |
| E3 | Run `dotnet test tests/ArchitectureTests`; fix any remaining `Features.*` → `DataAccessLayer.Repos` edges (grep). |
| E4 | Document rule in `AI_GUIDELINES.md`: *feature code depends on application interfaces, not `DataAccessLayer.Repos` namespace*. |

---

## Suggested order

1. **E** (small, unblocks CI architecture test) — can be one PR.
2. **A** (frontend parallelization; no backend risk) — multiple small PRs by entity.
3. **C** (contract versioning) — coordinate API + worker release.
4. **B** (controller moves) — steady cadence.
5. **D** (docs/scripts) — ongoing.

---

## Success criteria

- `dotnet test` passes `Features_ShouldNotDependOn_DataAccessLayer_Repos`.
- `client.ts` reduced to re-exports + any truly shared glue; entity APIs in dedicated files.
- `FullSyncMessage` (or successor) carries explicit version; worker documents behavior for unknown versions.
- README/checklists give a single entry point for DB + smoke expectations.
