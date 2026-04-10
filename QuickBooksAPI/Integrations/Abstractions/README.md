# Accounting integration abstractions

Interfaces here describe **what the application needs** from an external general ledger (vendor sync, etc.) without naming Intuit-specific APIs.

- Implementations live under sibling folders, e.g. `Integrations/QuickBooks/`.
- A second provider (e.g. Xero) would add `Integrations/Xero/` with its own HTTP client and mappers, implementing the same interfaces; core services in `Services/` should depend only on these abstractions at the boundary being migrated.

Current pilots: `IVendorAccountingSyncGateway` (vendor QBO pull), `IProductAccountingSyncGateway` (product/item QBO query pages during sync), each with a `QuickBooks*` adapter.
