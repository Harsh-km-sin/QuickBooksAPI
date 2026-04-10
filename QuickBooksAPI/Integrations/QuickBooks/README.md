# QuickBooks Online integration

This folder holds **DI registration** for the QuickBooks Online HTTP/OAuth adapters implemented in the **`QuickBooksService`** project (`IQuickBooks*Service` implementations).

Application orchestration and domain façades live under `QuickBooksAPI/Services/`. Add new QBO adapter registrations in `QuickBooksIntegrationServiceCollectionExtensions.cs`.
