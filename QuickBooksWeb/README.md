# QuickBooksWeb - MVC Frontend

This is the MVC frontend for QuickBooksAPI. It runs as a separate project and communicates with the API via HTTP.

## Setup

1. **Run both projects**
   - Start QuickBooksAPI (https://localhost:7135)
   - Start QuickBooksWeb (http://localhost:5212)

2. **QuickBooks OAuth redirect**
   - Add `http://localhost:5212/QuickBooks/Callback` to your QuickBooks app's allowed redirect URIs in the [Intuit Developer Portal](https://developer.intuit.com/).
   - The API's `QuickBooks:RedirectUri` in appsettings.Development.json is set to this URL when using the MVC app.

3. **API base URL**
   - Configured in appsettings.json under `ApiSettings:BaseUrl` (default: https://localhost:7135).

## Usage

1. Register a new account or login.
2. Connect QuickBooks (nav link) to authorize the app with your QuickBooks company.
3. Visit Products or Customers and click "Sync from QuickBooks" to import data.
4. List views show synced data from the local database.
