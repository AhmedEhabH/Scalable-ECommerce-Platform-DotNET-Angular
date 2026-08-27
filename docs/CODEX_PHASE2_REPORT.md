# Execution Identity

- Codex version: workspace/API Codex agent (CLI build version unavailable in this environment)
- model: GPT-5
- date: 2026-08-27
- branch: `recovery/ui-portfolio`
- start HEAD: `0fcf5596d3f51c51c2ec1225fdcac05842484081`
- end implementation HEAD: `7fcd9782f6d0d7555e5e9453dd045accee5555b3` (the documentation-only report commit follows this implementation HEAD)

# Changes Made

## Reproducible SDK selection

- Problem: .NET 8 projects implicitly selected installed SDK 10.
- Root cause: the repository had no root `global.json`.
- Files changed: `global.json`.
- Solution: pinned installed stable SDK `8.0.423` with `rollForward: latestPatch`.
- Why correct: every project remains targeted to .NET 8, SDK 10 is excluded, and compatible .NET 8 servicing patches remain usable.

## Angular dependency consistency

- Problem: `npm ci` failed because Angular framework peers resolved to different exact patches; `ng2-charts` also allowed npm to auto-select incompatible Angular CDK 22.
- Root cause: caret ranges plus stale lock entries resolved animations to 21.2.13, the remaining framework to 21.2.7, and the unbounded CDK peer to 22.
- Files changed: `frontend/package.json`, `frontend/package-lock.json`.
- Solution: exact-pinned Angular framework packages, compiler CLI, and CDK to `21.2.13`; regenerated the lockfile and verified it with clean `npm ci` without legacy/force flags.
- Why correct: Angular stays on major 21, all exact framework peer edges align, and unrelated direct dependencies were not broadly upgraded.

## HttpOnly-cookie review authentication

- Problem: `ReviewService` called removed `AuthService.getToken()` and built JavaScript Bearer headers.
- Root cause: the service retained a pre-cookie access-token assumption after the application migrated access authentication to an HttpOnly cookie.
- Files changed: `frontend/src/app/features/products/services/review.service.ts`, `frontend/src/app/core/guards/auth.guard.spec.ts`.
- Solution: removed token lookup and manual Authorization headers; review calls now flow through the existing interceptor, which sets `withCredentials: true`. Guard fixtures now model the current persisted user state and use a Router test double.
- Why correct: JavaScript never receives or reads the access token; the backend JWT handler reads `access_token` from the cookie, CORS allows credentials, and SignalR retains its intentional query-token exception.

## Frontend test setup

- Problem: category specs lacked router context, the edit-form test injected state that `ngOnInit` immediately overwrote, and guard redirects produced NG04002 errors.
- Root cause: standalone `RouterLink` requires router providers; category edit mode is sourced from `ActivatedRoute`; empty real router configuration could not resolve redirect targets.
- Files changed: `frontend/src/app/features/admin/pages/admin-categories.component.spec.ts`, `frontend/src/app/features/admin/pages/admin-category-form.component.spec.ts`, `frontend/src/app/core/guards/auth.guard.spec.ts`.
- Solution: supplied router providers where template directives need them, supplied an actual route param map for edit mode, and isolated guard navigation with a Router test double.
- Why correct: assertions still exercise actual component/guard behavior; no test was skipped, deleted, or weakened.

## Platform-independent API test hosting

- Problem: integration hosts attempted privileged Windows Event Log initialization; after that was exposed, the Hangfire authorization test also lacked JWT validation key material.
- Root cause: default Windows logging providers were active in Testing and one test did not select the Testing environment. Authentication options are lazily initialized on the Hangfire request and production correctly rejects missing key configuration.
- Files changed: `src/ECommerce.Api/Program.cs`, `tests/ECommerce.Api.Tests/Integration/HangfireConfigurationTests.cs`.
- Solution: Testing clears default logging providers and adds console logging; Hangfire explicitly uses Testing; Testing gets an ephemeral RSA validation key only when no configured public key exists. The Hangfire assertion remains 401 and now includes response diagnostics on failure.
- Why correct: production logging, observability, and mandatory production JWT key configuration remain intact while tests require neither Event Log privilege nor external secrets.

## CI coverage

- Problem: CI validated only the backend.
- Root cause: the workflow had a single .NET job.
- Files changed: `.github/workflows/ci.yml`.
- Solution: retained backend restore/build/test and added a Node 24 frontend job with cached clean install, production build, and non-watch tests.
- Why correct: Node 24 satisfies Angular 21's declared engine and matches the validated local Node major; both application halves now gate changes.

# Dependency Resolution

Before:

- `@angular/animations`: 21.2.13
- `@angular/common`: 21.2.7
- `@angular/compiler`: 21.2.7
- `@angular/compiler-cli`: 21.2.7
- `@angular/core`: 21.2.7
- `@angular/forms`: 21.2.7
- `@angular/platform-browser`: 21.2.7
- `@angular/router`: 21.2.7
- `@angular/cdk`: not pinned; npm attempted incompatible 22.1.4 for the `ng2-charts` peer
- `@angular/build` and `@angular/cli`: 21.2.6

After:

- `@angular/animations`, `@angular/cdk`, `@angular/common`, `@angular/compiler`, `@angular/compiler-cli`, `@angular/core`, `@angular/forms`, `@angular/platform-browser`, and `@angular/router`: exactly 21.2.13
- `@angular/build` and `@angular/cli`: unchanged at 21.2.6

# Authentication Audit

- `ReviewService.getAuthHeaders()`, `AuthService.getToken()`, and its `Authorization: Bearer` construction: **STALE**, removed.
- `auth.guard.spec.ts` writes to `localStorage.access_token`: **STALE**, removed; current guards use restored user state because the HttpOnly token is intentionally unreadable.
- `auth.interceptor.ts` `withCredentials: true` and corresponding interceptor specs: **VALID**, retained; this is the browser-to-cookie transport mechanism.
- Interceptor spec assertion that no `Authorization` header is added: **VALID**, retained as a regression guard.
- `AuthService` storage of `auth_user`: **VALID** for client display/role state; it is not an access token and is not the backend authorization source.
- Theme, last-order, and wishlist localStorage usage: **VALID**, unrelated application state.
- `AuthService` storage of `refresh_token` in localStorage/sessionStorage and matching specs: **SECURITY RISK**, not a stale access-token assumption and therefore not changed in this phase. A future auth-hardening decision should consider migrating refresh tokens to an HttpOnly cookie too.
- Frontend source now contains no `getToken()`, access-token local/session storage, or runtime Bearer-header construction.

# Backend Validation

Validated under SDK `8.0.423`:

- `dotnet restore`: PASS; all seven projects restored/up to date.
- `dotnet build --configuration Release`: PASS; 0 errors, 31 warnings.
- `dotnet test --configuration Release`: PASS (exit code 0); Application 108/108 and API 79/79, total 187 passed, 0 failed, 0 skipped. `ECommerce.Domain.Tests` currently discovers no tests.
- Focused repaired tests: PASS, 2/2 (`ObservabilityTests.TracerProvider_ShouldResolve_WithoutCrashing` and `HangfireConfigurationTests.HangfireDashboard_ShouldReturn401_ForUnauthenticatedRequest`).

# Frontend Validation

- `npm ci`: PASS from a clean dependency tree without `--legacy-peer-deps` or `--force`; 490 packages installed.
- `npm run build`: PASS; production output generated. Existing initial-bundle budget warning remains: 845.11 kB versus 500.00 kB budget.
- `npm test -- --watch=false`: PASS; 6 files, 33/33 tests, 0 failed.

# CI Validation

CI now executes two independent Ubuntu jobs:

- Backend: checkout, .NET 8 setup, `dotnet restore`, Release `dotnet build --no-restore`, Release `dotnet test --no-build`.
- Frontend: checkout, Node 24 setup with npm lockfile cache, `npm ci`, `npm run build`, `npm test -- --watch=false` from `frontend`.

# Remaining Warnings

- Backend Release build has 31 nullable warnings. Credible production-risk warnings are `Product.DiscountPercentage` dereferencing nullable `CompareAtPrice`, an uninitialized `PaginatedResult<T>.Items` JSON-constructor path, and possible null `ProductDto` insertion in admin-summary and products-controller result handling. ProductService logging argument warnings may produce poor diagnostics but are lower runtime risk. The remaining warnings are test assertion dereferences. No broad nullable refactor was attempted.
- `ECommerce.Domain.Tests` builds but contains/discovers no tests, leaving domain behavior without that project-level test coverage.
- Frontend production build exceeds its initial bundle budget by 345.11 kB (845.11 kB total versus 500.00 kB).
- Clean npm install reports 30 audit findings: 3 low, 5 moderate, 21 high, and 1 critical. No broad dependency/audit upgrade was performed because it is outside the minimum Angular consistency repair.

# Git State

Implementation commits created:

- `e1c570944aa2da09fd134075b16af50110172063` — `chore: pin project SDK and align frontend dependencies`
- `54e29fd1b3efbe66fd3a505187749f980f719d36` — `fix(auth): complete HttpOnly cookie migration`
- `4511e25174a2ccc57b0277df2db54bc6bba75940` — `test: stabilize frontend and API test hosts`
- `7fcd9782f6d0d7555e5e9453dd045accee5555b3` — `ci: validate frontend and backend`

Before the report file was added, `git status --short` was empty and `git diff --check` passed. The report is committed separately as documentation. No generated build artifacts or secrets are tracked. Nothing was pushed or merged; `main` was not modified.

# Phase 3 Readiness

Yes. The repository has a reproducible .NET 8 SDK selection, a clean-installable Angular graph, green production builds, green discovered tests, cookie-consistent review authentication, deterministic API test hosting, and CI coverage for both halves. UI recovery can safely begin after external approval, with the listed warnings tracked for later work.

# STOP

UI redesign has **NOT** started. No homepage, header, product-card, design-system, theme, CSS, README portfolio, or architecture redesign/removal work was performed. Phase 2 stops here pending approval for Phase 3.
