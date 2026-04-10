API feature slices live here (`QuickBooksAPI/Features/`) so they stay in the main web project. The repo root `Features/` folder is not used for application code.

| Slice | Entry point | Notes |
|--------|-------------|--------|
| **Auth** | [Auth/README.md](Auth/README.md) | `AuthController` — `api/auth/*`. |
| **Companies** | [Companies/README.md](Companies/README.md) | `CompanyController` — `api/company/*`. |
| **Customers** | `Customers/CustomerController.cs` | `api/customer/*`. |
| **Products** | `Products/ProductController.cs` | `api/product/*`. |
| **Vendors** | `Vendors/VendorController.cs` | `api/vendor/*`. |
| **Bills** | `Bills/BillController.cs` | `api/bill/*`. |
| **Invoices** | `Invoices/InvoiceController.cs` | `api/invoice/*`. |
| **ChartOfAccounts** | `ChartOfAccounts/ChartOfAccountsController.cs` | `api/chartofaccounts/*`. |
| **JournalEntries** | `JournalEntries/JournalEntryController.cs` | `api/journalentry/*`. |
| **Analytics** | `Analytics/AnalyticsController*.cs` | `api/analytics/*`. Split as **partial** classes (`AnalyticsController.cs` + `AnalyticsController.*.cs`) to keep files under CI size limits. |
| **CfoAssistant** | `CfoAssistant/CfoAssistantController.cs` | `api/cfo-assistant/*`. |
| **Forecast / CloseIssues** | `Forecast/`, `CloseIssues/` | Services only (routes live under `api/analytics/*`). |

Frontend counterparts: `QuickBooksAPI Frontend/app/src/features/auth/`, `…/features/company/`, and domain APIs under `app/src/api/*Api.ts`.

### Next steps (maintainers)

- **Architecture tests:** `Features_ShouldNotDependOn_DataAccessLayer_Repos` — feature code uses `Application.Interfaces` for repos; see [`AI_GUIDELINES.md`](../../AI_GUIDELINES.md).
