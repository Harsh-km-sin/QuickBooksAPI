# Phase 3 Track B — API smoke checklist

Use a valid JWT (`Authorization: Bearer …`) and a connected realm (`X-Realm-Id` or whatever your API uses for `IRequestContext.RealmId` — match `CurrentUserMiddleware` / client behavior).

Base path: **`/api/Analytics`** (controller name; routes are lowercase in typical ASP.NET Core).

| # | Method | Route | Expect |
|---|--------|-------|--------|
| 1 | POST | `/api/Analytics/forecast` | 200 + scenario id; body per `CreateForecastRequest` |
| 2 | GET | `/api/Analytics/forecast/{id}` | 200 + scenario + results, or 404 if wrong id |
| 3 | GET | `/api/Analytics/close-issues` | 200 + list (may be empty) |
| 4 | POST | `/api/Analytics/close-issues/{id}/resolve` | 200 for valid id |
| 5 | GET | `/api/Analytics/entities` | 200 + entities for user |
| 6 | GET | `/api/Analytics/consolidated-pnl?entityId=&from=&to=` | 200 or 400/404 per validation |
| 7 | POST | `/api/cfo-assistant/ask` | 200 + answer payload (see `CfoAssistantController`) |

**Frontend parity:** `Forecast`, `CloseAssistant`, `CfoAssistant` pages; Dashboard consolidation toggle (uses `analyticsApi`).

If any call fails with SQL object missing errors, apply `PHASE3_DATABASE_CHECKLIST.md` scripts first.
