# Full sync worker — end-to-end smoke

Validates the path: **API (or manual)** → **Azure Service Bus queue `qbo-full-sync`** → **`FullSyncWorker`** → **`IFullSyncOrchestrator`** → **sync status in SQL**.

## Prerequisites

| Requirement | Notes |
|---------------|--------|
| Azure Service Bus | Namespace with queue **`qbo-full-sync`** (or the name in `ServiceBus:QueueName`). |
| API configuration | `ServiceBus:ConnectionString` set (otherwise API uses `NoOpQueuePublisher` and no message is sent). |
| SyncWorker configuration | `ServiceBusConnection` in `local.settings.json` (local) or app settings (Azure). Same namespace/queue as API. |
| SQL | `DefaultConnection` — same database as API; tables for sync status, tokens, QBO entities. |
| QBO | Test **realm** must be connected with valid tokens so sync steps can call Intuit. |

## Message contract

Payload is JSON for `QuickBooksAPI.DataAccessLayer.Models.FullSyncMessage`. The API publisher uses `JsonSerializer.Serialize` with **default** naming (**PascalCase** property names):

```json
{
  "CompanyId": "<QBO realm id>",
  "UserId": "<app user id as string>",
  "RequestedAt": "2026-04-03T12:00:00Z",
  "CorrelationId": "optional-trace-id"
}
```

If you hand-author JSON for the queue, match **PascalCase** unless you configure `PropertyNamingPolicy` on both ends.

## Path A — Trigger from API (recommended)

1. Sign in and obtain a JWT.
2. Call the company full-sync endpoint used by the product (e.g. `POST` with body containing realm/company identity per `CompanyController` / `SyncRequestDto`).
3. Confirm in DB or logs: sync status for that realm moves **Queued** → **Running** → terminal state.
4. Watch **SyncWorker** logs for:
   - `Received sync message: ...`
   - `Full sync starting CorrelationId=...`
   - Completion or structured error.

## Path B — Send message manually (Service Bus Explorer / Azure CLI)

1. Build JSON with real `companyId` (realm) and `userId` (numeric user id as string).
2. Send to queue **`qbo-full-sync`** using the same connection string as the worker.
3. Ensure no concurrent sync is already **Running** for that company (`ISyncStatusRepository`), or the API path may refuse to queue.

Example (Azure CLI — adjust resource names):

```bash
az servicebus queue send --resource-group <rg> --namespace-name <namespace> --name qbo-full-sync --body "{\"CompanyId\":\"<realm>\",\"UserId\":\"1\",\"RequestedAt\":\"2026-04-03T12:00:00Z\"}"
```

## Success criteria

- Worker completes the Service Bus message (**no abandon**; **CompleteMessageAsync** on success).
- Sync status ends **Completed** or a documented partial-failure state (per orchestrator logic), not stuck **Running**.
- Logs show **`IFullSyncCompletedSubscriber`** activity (e.g. `LoggingFullSyncCompletedSubscriber` line) when sync finishes successfully.

## Failure criteria / debugging

- **Invalid sync message**: worker logs error; message may be dead-lettered after retries.
- **Exception in orchestrator**: status set to **Failed** with error message (see `FullSyncWorker` catch path).
- **Lock renewal**: long syncs rely on `host.json` `maxAutoLockRenewalDuration` (30 minutes) — extend if full sync regularly exceeds that.

## After you run this smoke

Check the box in `PHASE2_IMPLEMENTATION_TRACKER.md` §2.2 / `PHASE3_IMPLEMENTATION_TRACKER.md` carry-over once verified in a real environment.
