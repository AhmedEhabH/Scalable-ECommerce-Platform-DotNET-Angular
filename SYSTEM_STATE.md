# System State & Architecture

> Single Source of Truth for the E-Commerce Platform (May 2026)

---

## 1. System Overview

| Layer | Technology | Version |
|---|---|---|
| **Backend** | .NET (ASP.NET Core) | 8.0 |
| **Frontend** | Angular | 21.2.x |
| **Database** | SQL Server (via EF Core) | 2022 |
| **Cache** | Redis (StackExchange.Redis) | Alpine |
| **AI** | Google Gemini API (primary) / OpenAI (fallback) | — |
| **Charts** | Chart.js + ng2-charts | latest |
| **Testing** | Vitest (frontend) / xUnit (backend) | — |
| **CI/CD** | GitHub Actions | — |
| **Containerization** | Docker / Docker Compose | — |

### Architecture Style

- **Clean Architecture** with 4 layers: Domain → Application → Infrastructure → Api
- **Not full CQRS**: Only one MediatR handler exists (`AdminDashboardSummaryQuery`); the rest uses direct service interfaces
- **Repository Pattern**: Separate repository classes for complex queries
- **Decorator Pattern**: Caching layer wraps services (`CachedProductService`, `CachedCategoryService`)
- **Strategy Pattern**: Multiple AI providers (Gemini, OpenAI) supported

### Dependency Flow

```
ECommerce.Api → ECommerce.Infrastructure → ECommerce.Application → ECommerce.Domain
```

---

## 2. Implemented Features

### Backend (API)

#### Authentication & Authorization
- [x] JWT-based login/register/refresh-token
- [x] Three roles: `User`, `Admin`, `Seller`
- [x] Token-based auth with refresh token rotation
- [x] BCrypt password hashing (work factor 12)
- [x] Rate limiting per endpoint group

#### Product Management
- [x] Full CRUD with pagination, filtering, sorting, search
- [x] Image upload to `wwwroot/images/`
- [x] Category hierarchy (parent/child)
- [x] Featured products, stock tracking, low-stock alerts
- [x] Slug + SKU uniqueness validation
- [x] Seller ownership enforcement
- [x] Anonymous access to product listing/details

#### Cart
- [x] Create/get cart by userId or sessionId
- [x] Add/update/remove items with stock validation
- [x] Clear cart

#### Order & Checkout
- [x] Checkout: creates order from cart in transaction
- [x] Tax (10%), shipping (free over $100)
- [x] Stock deduction, cart clearing
- [x] Full order lifecycle (Pending → Confirmed → Processing → Shipped → Delivered → Cancelled → Refunded)
- [x] State machine validation on status transitions
- [x] Order history per user

#### Payment
- [x] Multiple payment methods (CreditCard, DebitCard, InstaPay, VodafoneCash, CashOnDelivery)
- [x] Payment lifecycle (Pending → Authorized → Paid → Failed → Refunded)
- [x] Amount validation against order total
- [x] Refund support

#### Reviews
- [x] CRUD with duplicate prevention
- [x] Verified purchase flag
- [x] Auto-updates product average rating
- [x] Review summary with star breakdown

#### Analytics
- [x] Dashboard summary (total orders, sales, products, users)
- [x] Top-selling products
- [x] Monthly sales trend by year
- [x] Recent orders + low-stock products

#### AI Assistant
- [x] Chat endpoint (`POST /api/ai/chat`)
- [x] Google Gemini integration with dynamic system prompt from product catalog
- [x] OpenAI fallback provider
- [x] Conversation history support
- [x] Resilience: retry (3x) + circuit breaker + 25s timeout

#### Admin
- [x] Dashboard summary endpoint
- [x] Full product/category management
- [x] Seller management

#### Sellers
- [x] Seller registration and approval flow
- [x] Own product management
- [x] Public seller page

#### Caching
- [x] Redis distributed cache via `IDistributedCache`
- [x] Decorator pattern: `CachedProductService`, `CachedCategoryService`
- [x] Cache-aside pattern with prefix-based invalidation
- [x] 30-minute default expiration

#### Middleware & Error Handling
- [x] Global exception handling middleware
- [x] FluentValidation auto-validation via action filter
- [x] Structured error responses (`ApiResponse<T>`)
- [x] Request rate limiting (4 policies)
- [x] CORS for Angular dev server
- [x] Health checks (`/health`, `/health/ready`)

#### Infrastructure
- [x] SQL Server with EF Core (retry-on-failure)
- [x] Redis caching
- [x] Serilog logging (console + file)
- [x] Docker + Docker Compose
- [x] GitHub Actions CI
- [x] Database seeding with sample data

#### Background Jobs (Hangfire)
- [x] Hangfire.AspNetCore + Hangfire.SqlServer installed and configured
- [x] SQL Server storage for jobs (`UseSqlServerStorage` with `DefaultConnection`)
- [x] Hangfire dashboard at `/hangfire` — secured with `HangfireAuthorizationFilter` (Admin role only)
- [x] `CartCleanupService` — removes carts where `UpdatedAt` is older than 7 days
- [x] Daily recurring job via `IRecurringJobManager.AddOrUpdate` using `Cron.Daily`
- [x] Integration test verifies `/hangfire` returns 401 for unauthenticated requests
- [x] Unit tests (`CartCleanupServiceTests`) verify cleanup logic with InMemory database

---

### Frontend (Angular)

#### Layout & Navigation
- [x] Sticky header with responsive hamburger menu
- [x] Footer with copyright
- [x] Route-based navigation (14+ routes)
- [x] Auth guards (`authGuard`, `adminGuard`, `sellerGuard`)
- [x] Not-found (404) and unauthorized (403) pages

#### Authentication
- [x] Login form with show/hide password, "remember me" (localStorage vs sessionStorage)
- [x] Registration form
- [x] JWT token storage and auto-attachment via interceptor
- [x] Auto-redirect to login on 401
- [x] Role-aware UI (Admin/Seller routes)

#### Product Browsing
- [x] Product listing with pagination (10 per page)
- [x] Search with 300ms debounce
- [x] Category, in-stock, featured filters
- [x] Sort: Featured, Price asc/desc, Name asc/desc
- [x] Product cards with badges (sale, top-rated, low stock, out of stock)
- [x] Product detail page with image, reviews, seller info
- [x] Star rating (display + interactive)

#### Cart
- [x] Cart panel with quantity controls (+/-)
- [x] Remove items
- [x] Order summary sidebar (subtotal, shipping, total)
- [x] Loading/empty/error states
- [x] Double-click prevention for updating items

#### Checkout
- [x] Reactive form with address fields
- [x] Order summary review
- [x] Form validation with error messages
- [x] Navigation to order-success on completion

#### Orders
- [x] Order list with status badges
- [x] Order detail view with items, addresses, payment info

#### Wishlist
- [x] Add/remove products
- [x] localStorage persistence
- [x] Reactive wishlist count badge in header
- [x] Dedicated wishlist page with grid display

#### Admin Dashboard
- [x] KPI cards with gradient backgrounds (Revenue, Orders, Users, Avg Order Value)
- [x] Monthly sales trend line chart (ng2-charts / Chart.js)
- [x] Top selling products table
- [x] Recent orders table

#### Admin Product Management
- [x] Products table with edit/delete
- [x] Create/edit form with image upload (5MB limit, client validation)
- [x] Shared between Admin and Seller roles
- [x] Meaningful error messages for SKU/slug/category conflicts

#### Admin Category Management
- [x] Categories table with display order
- [x] Create/edit form
- [x] Delete validation (prevents deletion if products exist, deactivates instead)

#### Theme System
- [x] 4 themes: Light, Dark, GitHub, GitHub Dark
- [x] CSS custom properties architecture
- [x] localStorage persistence
- [x] System preference detection (`prefers-color-scheme`)
- [x] Smooth theme transitions

#### AI Shopping Assistant
- [x] Floating action button opens chat panel
- [x] Rule-based query parser (budget, gaming, laptops, phones, in-stock, featured, etc.)
- [x] Backend AI integration (`POST /api/ai/chat`)
- [x] Suggestion chips, typing indicator, message history

#### Toast Notifications
- [x] Signal-based toast service
- [x] Success/error/info variants with icons
- [x] Auto-dismiss + manual close
- [x] Slide-in animation

#### UI/UX
- [x] Product image fallback pipe (local assets → URL validation → API path → placeholder)
- [x] Responsive design (mobile-first)
- [x] Loading, empty, and error states throughout
- [x] Consistent CSS variable theming

---

## 3. Design Patterns Used

| Pattern | Location | Purpose |
|---|---|---|
| **Clean Architecture** | Solution structure | Separation of concerns across 4 layers |
| **Repository Pattern** | `ECommerce.Infrastructure/Repositories/` | Data access abstraction for complex queries |
| **Decorator Pattern** | `CachedProductService`, `CachedCategoryService` | Add caching behavior without modifying core services |
| **Cache-Aside** | Cached services + `ICacheService` | Read from cache, fall back to DB, populate cache |
| **Strategy Pattern** | `AiChatService` | Switch between Gemini and OpenAI providers |
| **Circuit Breaker** | HTTP resilience pipeline | Prevent cascading failures in AI API calls |
| **Retry Pattern** | `AddStandardResilienceHandler` (3 retries) + EF Core retry (3x) | Handle transient failures |
| **Result Pattern** | `Result<T>` in Application layer | Explicit success/failure returns without exceptions |
| **Unit of Work** | `ApplicationDbContext` (EF Core) | Transactional consistency across repositories |
| **State Machine** | `Order.UpdateStatus()`, `Payment` lifecycle | Controlled status transitions with validation |
| **Mediator (limited)** | `AdminDashboardSummaryQuery` + Handler | Single CQRS query for admin dashboard |
| **Guard Pattern** | Angular route guards | Protect routes by auth/role |
| **Interceptor Pattern** | Angular HTTP interceptor | Attach JWT headers, handle 401 globally |
| **Signal Pattern** | Angular signals for state | Reactive UI state (theme, toast, wishlist, cart) |
| **Pipe Pattern** | `ProductImagePipe` | Transform image URLs with fallback chain |

---

## 4. External Integrations

| Integration | Purpose | Configuration |
|---|---|---|
| **SQL Server 2022** | Primary database | Connection string in `appsettings.json` / Docker Compose |
| **Redis (Alpine)** | Distributed caching | `localhost:6379` (dev) / `redis:6379` (Docker) |
| **Google Gemini API** | AI chat assistant | `Ai:Provider = Gemini`, `Gemini:ApiKey`, `Gemini:Endpoint` |
| **OpenAI API** | AI chat fallback provider | `Ai:Provider = OpenAI`, `OpenAI:ApiKey`, `OpenAI:Model` |
| **BCrypt** | Password hashing | Work factor 12 |
| **Serilog** | Structured logging | Console + rolling file sink |
| **JWT Bearer** | Authentication | Symmetric key, configurable issuer/audience/expiry |
| **Slugify (custom)** | URL slug generation | Custom extension method |

### API Endpoints Reference

All backend routes are prefixed with `/api` (e.g., `/api/products`, `/api/auth/login`).

---

## 5. Known Constraints & Workarounds

### Backend

| Constraint | Workaround |
|---|---|
| EF Core cannot translate computed properties (`OrderItem.Total`) | Use inline arithmetic `(Price - Discount) * Quantity` in LINQ queries |
| Complex model binding on `[FromQuery]` can cause 400 | Use simple primitive types (`int year = 2026`) with defaults |
| Chart.js v3+ tree-shakes scale registrations | Call `Chart.register(...registerables)` before rendering |

### Frontend

| Constraint | Workaround |
|---|---|
| `localStorage` unavailable during SSR | Check `isPlatformBrowser()` before accessing storage |
| JWT token decoding requires browser `atob()` | Falls back to empty string if unavailable |
| Chart.js v3+ requires explicit registration | `Chart.register(...registerables)` in component module scope |

### Architecture Notes

- **No full CQRS**: Despite README mention, only one MediatR handler (`AdminDashboardSummaryQuery`) exists. The rest uses direct service interfaces.
- **No SSR**: The Angular app is purely client-side rendered. No `main.server.ts` or `provideServerRendering` exists.
- **No API Gateway**: The frontend communicates directly with the backend API. No BFF or gateway layer.
- **Single cache tier**: Redis is used for caching but not for session state or real-time features.
- **No background jobs**: No Hangfire, Quartz, or similar scheduling for async tasks.
- **No event bus**: No message queue (RabbitMQ, Azure Service Bus) for domain events.
- **No WebSocket/SignalR**: No real-time updates (e.g., order status push).
- **No localization/i18n**: English-only UI.
- **No PWA/offline support**: No service worker or offline caching.
- **Wishlist is client-only**: Stored in localStorage, not synced to server.
