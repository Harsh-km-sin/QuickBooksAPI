# SyncWorker timer schedules (source: code)

| Function | CRON in code | README example |
|----------|----------------|----------------|
| `KpiSnapshotFunction` | `0 0 2 * * *` | Daily 02:00 UTC |
| `CloseIssuesFunction` | `0 0 3 * * *` | Daily 03:00 UTC |
| `ConsolidationFunction` | `0 0 4 1 * *` | Monthly, day 1, 04:00 UTC |

All use `RunOnStartup = false`. Change triggers in the `[TimerTrigger(...)]` attribute if you need different schedules; keep README in sync.
