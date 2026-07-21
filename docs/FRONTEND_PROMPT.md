# Frontend Creation Prompt: QuickBooks Accounting Integration Platform

You are a senior full-stack architect and UI/UX expert. Your task is to design and build a production-ready frontend for a QuickBooks Online accounting integration platform.

---

## Project Goal

Create a professional web application that allows users to:
- Register and sign in
- Connect their QuickBooks Online company/companies via OAuth 2.0
- Manage accounting entities (Customers, Products, Vendors, Bills, Invoices, Chart of Accounts, Journal Entries) synced with QuickBooks
- Perform CRUD operations and sync data with the backend API

The frontend must integrate seamlessly with an existing .NET 8 backend API. Refer to the project README.md for complete API documentation.

---

## Target Audience

- Small business owners using QuickBooks Online
- Bookkeepers and accountants managing client books
- Business users who need to view and manage accounting data
- Users who may have multiple QuickBooks companies (multi-tenant)

---

## Backend API Reference

**Base URL (Development):** `https://localhost:7135`  
**Swagger UI:** `https://localhost:7135/` (root)

All API responses use the `ApiResponse<T>` wrapper:
```json
{
  "success": true,
  "message": "Optional message",
  "data": { },
  "errors": ["Optional array of error details"]
}
```

**Authentication:**
- JWT Bearer token returned from Login
- Header: `Authorization: Bearer <token>`
- Realm (QuickBooks company): Send `X-Realm-Id: <realmId>` header when user has multiple companies
- Token claims: `UserId`, `RealmIds` (JSON array), `Name` (username)
- Token expiry: 2 hours

**QuickBooks OAuth Flow:**
1. User logs in (gets JWT)
2. Call `GET /api/auth/oAuth` with Bearer token → returns OAuth URL
3. Redirect user to URL
4. QuickBooks redirects to backend callback with `code`, `state`, `realmId`
5. Callback saves token; frontend can store realmId for X-Realm-Id
6. For multi-company: JWT includes RealmIds; frontend selects realm and sends X-Realm-Id on all requests

---

## Platform Requirements

### 1. Authentication Module

- **Sign Up:** `POST /api/auth/SignUp` - Body: `{ firstName, lastName, username, email, password }` - Password: 8–100 chars, 1 uppercase, 1 lowercase, 1 number, 1 special character
- **Login:** `POST /api/auth/login` - Body: `{ email, password }` - Store JWT (memory or secure storage)
- **Protected Routes:** All entity pages require valid JWT; redirect to login if missing/expired
- **Logout:** Clear token, redirect to login
- **Token Refresh:** Handle 401; clear token and redirect to login (backend token is 2hr; no refresh endpoint)

### 2. QuickBooks Connection Flow

- **Connect QuickBooks:** Button that calls `GET /api/auth/oAuth`, then redirects user to returned URL
- **Callback Handling:** Backend handles `/api/auth/callback`; frontend may need a post-callback page or redirect
- **Connection Status:** Display whether QuickBooks is connected for the current realm
- **Reconnect:** Allow reconnecting if disconnected

### 3. Multi-Company (Realm) Support

- **Realm Switcher:** If user has multiple RealmIds in JWT, show company selector (dropdown/sidebar)
- **Persist Selection:** Store selected realmId in state/context; send as `X-Realm-Id` on every API request
- **Scope All Data:** All entity lists and actions are scoped to the selected realm

### 4. Entity Management

Integrate with all backend endpoints. For each entity with CRUD:

| Entity | List | Sync | Create | Update | Delete |
|--------|------|------|--------|--------|--------|
| Customer | GET /api/customer/list | GET /api/customer/sync | POST /api/customer/create | PUT /api/customer/update | DELETE /api/customer/delete |
| Product | GET /api/product/list | GET /api/product/sync | POST /api/product/create | PUT /api/product/update | DELETE /api/product/delete |
| Vendor | GET /api/vendor/list | GET /api/vendor/sync | POST /api/vendor/create | PUT /api/vendor/update | DELETE /api/vendor/softDelete |
| Bill | GET /api/bill/list | GET /api/bill/sync | POST /api/bill/create | PUT /api/bill/update | DELETE /api/bill/delete |
| Invoice | GET /api/invoice/list | GET /api/invoice/sync | POST /api/invoice/create | PUT /api/invoice/update | DELETE /api/invoice/delete, POST /api/invoice/void |
| Chart of Accounts | GET /api/chartofaccounts/list | GET /api/chartofaccounts/sync | - | - | - |
| Journal Entry | GET /api/journalentry/list | GET /api/journalentry/sync | - | - | - |

- **List Pages:** Table/grid with sort, filter, search, pagination
- **Sync Button:** "Sync from QuickBooks" triggers sync endpoint; show loading + success message (e.g. "X records synced")
- **Create/Edit Forms:** Modal or separate page with validation
- **Delete:** Confirmation dialog; use correct delete/softDelete per entity
- **Entity Types:** Use TypeScript interfaces from README (Customer, Products, Vendor, QBOBillHeader, QBOInvoiceHeader, ChartOfAccounts, QBOJournalEntryHeader)

### 5. Forms and Request DTOs

Implement forms matching backend request schemas from README:
- **Customer:** CreateCustomerRequest, UpdateCustomerRequest, DeleteCustomerRequest (nested: CreateEmailDto, CreatePhoneDto, CreateAddressDto)
- **Product:** CreateProductRequest, UpdateProductRequest, DeleteProductRequest (Reference/IncomeAccountRef)
- **Vendor:** CreateVendorRequest, UpdateVendorRequest, SoftDeleteVendorRequest (VendorEmailAddr, VendorPhone, VendorBillAddr)
- **Bill:** CreateBillRequest (Line[], VendorRef, TxnDate, DueDate, CreateBillLineRequest, AccountBasedExpenseLineDetail, ItemBasedExpenseLineDetail), UpdateBillRequest, DeleteBillRequest
- **Invoice:** CreateInvoiceRequest (Line[], CustomerRef, CreateInvoiceLineRequest, SalesItemLineDetail), UpdateInvoiceRequest, DeleteInvoiceRequest, VoidInvoiceRequest

Validate required fields; display API error messages from `errors` array.

### 6. API Client and Response Handling

- **Centralized API Client:** Axios or fetch wrapper with base URL, interceptors for Authorization + X-Realm-Id
- **Response Parsing:** Always check `response.data.success`; use `response.data.data` on success; use `response.data.message` and `response.data.errors` on failure
- **Error Handling:** 401 → clear token, redirect to login; 403 → show "Access denied"; 429 → show rate limit message; 500 → generic error with optional X-Correlation-Id
- **CORS:** Backend must have frontend origin in Cors:AllowedOrigins (e.g. http://localhost:5173 for Vite)

### 7. UI/UX Design

- **Layout:** Sidebar navigation (Dashboard, Customers, Products, Vendors, Bills, Invoices, Chart of Accounts, Journal Entries, Settings/Connect QuickBooks)
- **Theme:** Dark/light mode toggle
- **Data Tables:** Sortable columns, filters, search, pagination
- **Forms:** Clear labels, inline validation, loading states on submit
- **Toasts/Notifications:** Success and error feedback
- **Loading States:** Skeletons or spinners for list/sync operations
- **Empty States:** "No data" and "Connect QuickBooks to get started" messaging
- **Aesthetic:** Professional, accounting/business app feel (clean, not overly decorative)

### 8. Dashboard

- **Summary Cards:** Counts for Customers, Products, Vendors, Bills, Invoices (call list endpoints or a future stats endpoint)
- **QuickBooks Status:** Connected / Not connected indicator
- **Quick Actions:** Connect QuickBooks, Sync all, links to entity pages

### 9. Data Visualization System

Include interactive charts and visualizations for financial/accounting data:
- **Invoice summary:** Total amounts, balances over time (line/bar charts)
- **Bill summary:** Vendor bills, amounts, due dates
- **Chart of Accounts:** Account balances, distribution (pie/bar)
- **Dashboard metrics:** Entity counts, totals, trends

**Libraries:** Recharts, Chart.js, or D3.js for charts. Recharts is recommended for React integration.

### 10. Scalability and Architecture

Design for:
- **Modular frontend:** Feature-based modules (auth, customers, products, bills, etc.)
- **API-based backend:** All data from existing .NET API; no direct DB access
- **Microservice-friendly:** Clear API boundaries; frontend can later talk to multiple services if needed
- **Clean folder structure:** `components/`, `pages/`, `hooks/`, `services/`, `stores/`, `types/`
- **Documentation-ready:** README, env example, component storybook (optional)

### 11. Branding

The application should feel like:
- **Professional accounting/business SaaS** (similar to QuickBooks, Xero, or FreshBooks)
- **Clean, trustworthy, data-focused** – not marketing-heavy or flashy
- **Technical documentation aesthetic** – clear hierarchy, readable typography, consistent spacing

---

## Technical Stack Requirements

**Frontend:**
- React 18+ with TypeScript
- **State Management:**
  - **Server State:** TanStack Query (React Query) for API data, caching, refetching, and sync status
  - **Client/UI State:** Zustand (lightweight) or Jotai for auth, realm selection, modals, sidebar state
  - **Form State:** React Hook Form + Zod for validation
- HTTP: Axios or fetch with centralized API client
- Styling: Tailwind CSS; optionally a component library (e.g. shadcn/ui, Radix UI)
- Animations: Framer Motion for page transitions and micro-interactions (optional)
- Charts: Recharts or Chart.js for dashboard visualizations
- Routing: React Router v6
- Tables: TanStack Table for advanced list features

**State Architecture:**
- **TanStack Query:** Queries for list endpoints; mutations for create/update/delete/sync; use `queryClient.invalidateQueries()` after mutations to refetch lists
- **Zustand/Jotai Store:** Auth (token, userId, username), Realm (current realmId, realmIds from JWT), UI (sidebar collapsed, theme, notification queue)
- Avoid Redux unless specifically required; Zustand or Jotai is sufficient for this scope

**Architecture:**
- Modular, feature-based folder structure
- Shared API client, types, and hooks
- Reusable UI components
- Environment-based API base URL (e.g. VITE_API_URL)

**Deployment:**
- Docker-ready (Dockerfile for frontend build)
- Cloud-ready (e.g. Vercel, Netlify, Azure Static Web Apps)
- Build: Vite or Create React App; output static assets for hosting

---

## Output Expectations

Provide:

1. **System Architecture:** High-level diagram of frontend modules (Auth, API Client, Entity pages, Layout)
2. **Folder Structure:** Suggested structure for components, pages, hooks, services, stores, types
3. **API Client:** Axios/fetch setup, interceptors, error handling, base URL config
4. **UI Page Layout Design:** Wireframe or description of main layout (sidebar, header, content area, realm switcher)
5. **Component Breakdown:** List of reusable components (Button, Table, Card, Modal, Form inputs, Toast, etc.)
6. **Auth Flow:** Login page, protected route wrapper, token storage, logout
7. **Entity Types:** TypeScript interfaces for all entities (from README)
8. **Example Pages:** Implement at least one list page (e.g. Customers), one create/edit form (e.g. Customer), and login
9. **Realm Switcher:** Component and integration with API client headers
10. **Error Handling:** Global handler for 401/403/429/500; toast/notification usage
11. **CORS Setup:** Note for backend Cors:AllowedOrigins to include frontend origin
12. **State Management Setup:**
    - TanStack Query provider and example queries/mutations for at least one entity (e.g. Customer)
    - Zustand/Jotai store for auth and realm with persistence (e.g. localStorage for realmId, sessionStorage for JWT if needed)
    - Example of invalidating queries after sync or create/update/delete mutation
13. **Example Interactive Visualization:** Dashboard chart (e.g. invoice/bill totals over time, or account balance distribution) using Recharts or Chart.js

---

## Quality Requirements

All solutions must:

- **Follow best software engineering practices:** Clear naming, DRY, single responsibility, consistent code style
- **Be production scalable:** Handle growth in users and data; efficient re-renders and API usage
- **Use modern UI design patterns:** Accessible components, clear feedback, predictable interactions
- **Be data-accurate:** Financial/accounting values (totals, balances) must display correctly; avoid rounding errors in calculations
- **Be optimized for performance:** Lazy loading, pagination for large lists, avoid unnecessary re-fetches
- **Type-safe:** Use TypeScript interfaces matching backend ApiResponse and entity models
- **Accessible:** Keyboard navigation, ARIA where appropriate
- **Responsive:** Works on desktop and tablet
- **Secure:** Never log or expose JWT; use httpOnly cookies if backend supports it

**If multiple design approaches exist** (e.g. state management choice, chart library, form library), compare them briefly and recommend the best one for this project context.

---

## Reference Documentation

The project README.md contains:
- Full endpoint reference table
- Request/response structures
- Entity TypeScript interfaces (copy-paste ready)
- Request DTO JSON schemas
- QuickBooks OAuth flow details
- Error handling and HTTP status codes

Use README.md as the single source of truth for API integration.

---

END PROMPT
