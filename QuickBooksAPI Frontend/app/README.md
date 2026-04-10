# QuickBooks Accounting Integration Platform - Frontend

A modern React frontend for the QuickBooks Online accounting integration platform. Built with TypeScript, Tailwind CSS, and shadcn/ui.

## Features

- **Authentication**: JWT-based login and registration
- **QuickBooks OAuth**: Connect and sync with QuickBooks Online
- **Multi-Company Support**: Switch between multiple QuickBooks companies (realms)
- **Entity Management**: Full CRUD operations for Customers, Products, Vendors, Bills, and Invoices
- **Read-Only Entities**: View Chart of Accounts and Journal Entries
- **Dashboard**: Visualize accounting data with interactive charts
- **Dark/Light Mode**: Theme toggle for user preference
- **Responsive Design**: Works on desktop and tablet devices

## Tech Stack

- **Framework**: React 18+ with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS
- **UI Components**: shadcn/ui (based on Radix UI)
- **State Management**: React Context API (`features/auth` for session and realm selection)
- **Data Fetching**: Custom hooks with a shared HTTP core (`api/core.ts`) and one module per domain (`api/*Api.ts`); optional barrel `api/client.ts` re-exports for convenience
- **Charts**: Recharts
- **Notifications**: Sonner toast notifications
- **Routing**: React Router v6

## Project Structure

Vertical slices for cross-cutting domains live under `src/features/` (e.g. **auth**, **company**). Shared HTTP helpers are in `api/core.ts`. Each domain exposes `api/<domain>Api.ts` (e.g. `customerApi.ts`, `analyticsApi.ts`). `api/client.ts` is an optional barrel that re-exports core + all `*Api` modules — prefer importing from `@/api/<name>Api` in app code for clearer ownership.

```
src/
├── api/
│   ├── core.ts            # Base URL, jwt/realm storage, apiClient, ApiError
│   ├── authApi.ts         # Login, sign-up, logout, QuickBooks OAuth URL/callback
│   ├── companyApi.ts      # Connected companies, sync, disconnect
│   ├── analyticsApi.ts    # CFO / dashboard analytics
│   ├── customerApi.ts … assistantApi.ts  # Entity + assistant APIs (one file per domain)
│   ├── listQuery.ts       # Shared list query string builder
│   └── client.ts          # Barrel: re-exports core + all *Api modules
├── features/
│   ├── auth/              # Auth slice: AuthProvider, useAuth, useQuickBooks, Login, Register
│   │   └── README.md
│   └── company/           # Company/realm slice: useConnectedCompanies, companyApi helpers
│       └── README.md
├── components/
│   ├── ui/                # shadcn/ui components
│   ├── MainLayout.tsx     # Main layout with sidebar and navigation
│   ├── ProtectedRoute.tsx # Route guard for authenticated routes
│   └── theme-provider.tsx # Dark/light mode provider
├── hooks/
│   ├── useCustomers.ts    # Customer data operations
│   ├── useProducts.ts     # Product data operations
│   ├── useVendors.ts      # Vendor data operations
│   ├── useBills.ts        # Bill data operations
│   ├── useInvoices.ts     # Invoice data operations
│   ├── useChartOfAccounts.ts
│   ├── useJournalEntries.ts
│   └── useDashboardStats.ts
├── pages/
│   ├── Dashboard.tsx
│   ├── ConnectedCompanies.tsx
│   ├── Customers.tsx
│   ├── Products.tsx
│   ├── Vendors.tsx
│   ├── Bills.tsx
│   ├── Invoices.tsx
│   ├── ChartOfAccounts.tsx
│   ├── JournalEntries.tsx
│   └── …                  # Other routed pages (see App.tsx)
├── pages/index.ts         # Re-exports pages; Login/Register come from @/features/auth
├── types/
│   ├── index.ts           # Re-exports domain type modules
│   ├── common.ts, auth.ts, customer.ts, …  # Domain-specific interfaces
├── App.tsx
└── main.tsx
```

See `src/features/auth/README.md` and `src/features/company/README.md` for slice boundaries and imports.

### Company slice (summary)

The **company / realm** vertical slice groups everything that lists QuickBooks-linked companies, drives **full sync** and **sync status**, **disconnect**, and **realm selection** (`X-Realm-Id`):

- **HTTP:** [`api/companyApi.ts`](src/api/companyApi.ts) — calls `api/company/*` (split out from the main `client` barrel for focused edits).
- **Feature code:** [`features/company/`](src/features/company/) — `useConnectedCompanies`, re-exports of `companyApi` and `setRealmId` / `getRealmId` from [`index.ts`](src/features/company/index.ts); consumed by **Connected Companies** and **MainLayout** (sidebar company switcher).
- **Backend:** `CompanyController` lives under `QuickBooksAPI/Features/Companies/` (not `Controllers/`).

Import auth vs company explicitly: `@/features/auth` and `@/features/company` (do not merge concerns in a single feature folder).

## Getting Started

### Prerequisites

- Node.js 18+ 
- npm or yarn
- Backend API running (see backend documentation)

### Installation

1. Install dependencies:
```bash
npm install
```

2. Configure environment variables:
```bash
cp .env.example .env
# Edit .env and set VITE_API_URL to your backend URL
```

3. Start the development server:
```bash
npm run dev
```

The app will be available at `http://localhost:5173` by default.

### Building for Production

```bash
npm run build
```

The build output will be in the `dist/` directory.

## API Integration

### Authentication

The frontend uses JWT Bearer tokens for authentication:
- Token is stored in `sessionStorage`
- Token is sent with every request in the `Authorization: Bearer <token>` header
- Token expiry is handled by redirecting to login

### Realm (Company) Selection

For users with multiple QuickBooks companies:
- Realm IDs are extracted from JWT claims
- Selected realm is stored in `localStorage`
- `X-Realm-Id` header is sent with every request

### API Response Format

All API responses follow the `ApiResponse<T>` wrapper:
```typescript
{
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}
```

### Error Handling

- 401: Clears token and redirects to login
- 403: Shows "Access denied" message
- 429: Shows rate limit message
- 500: Shows generic error with optional correlation ID

## QuickBooks OAuth Flow

1. User clicks "Connect QuickBooks" button
2. Frontend calls `GET /api/auth/oAuth` to get authorization URL
3. User is redirected to QuickBooks authorization page
4. After authorization, QuickBooks redirects to backend callback
5. Backend exchanges code for tokens and stores them
6. Frontend can now sync data from QuickBooks

## Entity Operations

### Customers, Products, Vendors
- List: View all entities with search and filtering
- Sync: Import from QuickBooks
- Create: Add new entities
- Update: Edit existing entities
- Delete/Soft Delete: Remove entities

### Bills, Invoices
- List: View all transactions
- Sync: Import from QuickBooks
- Create/Update/Delete: Manage transactions
- Void (Invoices only): Void an invoice

### Chart of Accounts, Journal Entries
- List: View all entries
- Sync: Import from QuickBooks
- Read-only: No create/update/delete operations

## Dashboard Charts

The dashboard includes:
- Entity count bar chart (Customers, Products, Vendors, Bills, Invoices)
- Financial overview pie chart (Invoices, Bills, Outstanding balances)
- Summary cards with key metrics

## Customization

### Adding New Entities

1. Add or extend TypeScript types in `src/types/<domain>.ts` and export from `src/types/index.ts`
2. Add API methods in `src/api/<feature>Api.ts` (and optionally re-export from `src/api/client.ts`)
3. Create custom hook in `src/hooks/`
4. Create page component in `src/pages/`
5. Add route in `src/App.tsx`
6. Add navigation item in `src/components/MainLayout.tsx`

Authentication and QuickBooks OAuth connection live in **`src/features/auth`** — do not add a second auth context under `context/`.

### Theming

The app uses CSS variables for theming. Edit `src/index.css` to customize colors.

## CORS Configuration

The backend must include the frontend origin in `Cors:AllowedOrigins`:

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "https://yourdomain.com"]
  }
}
```

## Deployment

### Docker

A Dockerfile can be added for containerized deployment:

```dockerfile
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Static Hosting

The `dist/` folder can be deployed to any static hosting service:
- Vercel
- Netlify
- Azure Static Web Apps
- AWS S3 + CloudFront
- GitHub Pages

## Development Notes

### Code Style

- Use TypeScript for type safety
- Follow React best practices (hooks, functional components)
- Use Tailwind CSS for styling
- Use shadcn/ui components when available

### State Management

- Server state: Custom hooks with fetch API
- Client state: React Context API (`AuthProvider` / `useAuth` from `@/features/auth`)
- Form state: Controlled components with useState

### Performance

- Components are optimized with useCallback and useMemo where appropriate
- Data fetching is cached at the hook level
- Large lists should implement virtualization (not yet implemented)

## License

MIT
