# Phase 2.5 Report — Portable, Test-Protected Green Baseline

### Identity

- Agent/tool: Codex Desktop `26.818.5229.0` (installed app package version)
- Exact model: not exposed by the runtime; no generic model label is substituted
- Reasoning/effort: not exposed by the runtime
- Date: 2026-08-27
- Branch: `recovery/ui-portfolio`
- Starting HEAD: `f47e2a8368d1fbb776122723ea2313ad472ffe04`
- Final HEAD: the commit containing this report; exact local/remote SHA is printed in the completion handoff

### Goal

Complete Phase 2.5 only: make the repository portable, correct stale auth/CI documentation, protect the explicitly selected critical behaviors, validate the full green gate, back up the recovery branch, create the first stable tag, and produce a lightweight external ZIP. Do not begin Phase 3.

### Starting state

- Phase 2 was complete locally on `recovery/ui-portfolio` at `f47e2a8`.
- Local `origin/main` was `0fcf5596d3f51c51c2ec1225fdcac05842484081`.
- No local tracking ref existed for `origin/recovery/ui-portfolio`; direct GitHub verification initially failed because `github.com:443` was unreachable.
- No tags existed.
- Four supplied handoff/contract documents were untracked: `AGENTS.md`, `CURRENT_STATE.md`, `PROJECT_EXECUTION_PLAN.md`, and `TEST_STRATEGY.md`.
- Discovered tests were Domain 0, Application 108, API 79, and frontend 33 across 6 files.
- The repository had no `.gitattributes`.
- README and `SYSTEM_STATE.md` described stale Bearer-header/client-token and CI/CD behavior.

### Scope promised

1. Repository portability and line-ending policy.
2. Accurate HttpOnly-cookie auth and CI documentation.
3. Required high-value Domain, HTTP API, and frontend tests.
4. Complete backend/frontend green gate.
5. Current-state, execution-plan, and phase-report updates.
6. Logical commits, branch push, remote SHA verification, stable tag, and external lightweight ZIP.

No UI recovery, new technology, architecture refactor, broad warning cleanup, or dependency upgrade was performed.

### Changes by feature

#### Portable repository checkout

- Problem: cross-platform extraction/checkouts produced mass line-ending noise and false dirty worktrees.
- Root cause: no repository-level line-ending policy existed.
- Files changed: `.gitattributes`.
- Behavior before: checkout behavior depended on each machine's Git settings.
- Behavior after: repository text uses LF, Windows-native launchers use CRLF, and common image/font/archive formats are binary.
- Tests added/updated: none; verified with `git check-attr`, diff review, and `git diff --check`.
- Verification result: no mass renormalization or semantic source rewrite was introduced.

#### Refresh-token rotation

- Problem: the required HTTP refresh scenario returned 400.
- Root cause: `AuthService.RefreshTokenAsync` asked EF Core to translate unmapped computed properties `RefreshToken.IsExpired` and `IsRevoked`.
- Files changed: `src/ECommerce.Infrastructure/Services/AuthService.cs`, `tests/ECommerce.Api.Tests/Integration/CriticalHttpFlowTests.cs`.
- Behavior before: a valid refresh token could fail at query translation.
- Behavior after: the EF query filters on persisted `ExpiresAt` and `RevokedAt`, then rotates the valid token normally.
- Tests added/updated: cookie login/protected endpoint/401/403/refresh HTTP scenario.
- Verification result: focused HTTP tests 5/5 and full API tests 84/84 pass.

#### Domain invariants

- Problem: the Domain test project discovered no tests.
- Root cause: important entity rules had no project-level regression protection.
- Files changed: `OrderTests.cs`, `ProductTests.cs`, `ReviewTests.cs`, `PaymentTests.cs` under `tests/ECommerce.Domain.Tests`.
- Behavior before: status, stock, discount, rating, and payment lifecycle regressions could pass the Domain project unnoticed.
- Behavior after: valid/invalid order transitions, nullable compare-price discounts, stock boundaries, review ratings, and payment/refund transitions are protected.
- Tests added/updated: 29 discovered Domain cases.
- Verification result: 29/29 pass.

#### Critical HTTP commerce and authorization flows

- Problem: most existing integration tests exercised services directly and did not prove the HTTP/auth/persistence boundary.
- Root cause: no focused in-memory `WebApplicationFactory` suite joined cookie auth, roles, controllers, services, and EF persistence.
- Files changed: `tests/ECommerce.Api.Tests/Integration/CriticalHttpFlowTests.cs`.
- Behavior before: login cookie attributes, cookie acceptance, checkout side effects, order ownership, product role ownership, structured validation, and HTTP order transitions were not protected together.
- Behavior after: five deterministic HTTP scenarios cover login cookie flags, cookie auth, 401/403, refresh, cart→checkout→order, stock deduction, cart clearing, insufficient stock, cross-user order denial, admin product CRUD, seller ownership denial, structured 400, and order-status role/transition rules.
- Tests added/updated: 5 HTTP integration tests using existing packages and the Testing environment.
- Verification result: 5/5 focused and 84/84 full API tests pass.

#### Frontend behavior protection

- Problem: header/cart/checkout/product-card behavior could be broken by upcoming template restructuring.
- Root cause: the frontend had no specs for these components.
- Files changed: four new `.spec.ts` files beside Header, Cart, Checkout, and ProductCard.
- Behavior before: role navigation/badges, mobile menu, cart mutation guards, checkout submit paths, and product-card events were unprotected.
- Behavior after: minimal behavioral tests cover these risks without snapshots or CSS assertions.
- Tests added/updated: 9 tests; total frontend suite increased from 33 to 42 tests and from 6 to 10 files.
- Verification result: 42/42 pass.

#### Accurate documentation and handoff

- Problem: README/System State described access-token storage/Bearer headers and a CI/CD deployment pipeline that do not exist.
- Root cause: documentation predated the HttpOnly-cookie migration and frontend CI job.
- Files changed: `README.md`, `SYSTEM_STATE.md`, `AGENTS.md`, `docs/CURRENT_STATE.md`, `docs/PROJECT_EXECUTION_PLAN.md`, `docs/TEST_STRATEGY.md`, `docs/PHASE2_5_REPORT.md`.
- Behavior before: readers received inaccurate security and delivery claims; Phase 2.5 source-of-truth documents were untracked.
- Behavior after: access-cookie transport, remaining refresh-token storage risk, CI-only validation, current counts, remaining risks, and next phase are explicit.
- Tests added/updated: documentation verified against source, test output, and CI workflow.
- Verification result: documentation claims match proved behavior.

### Test matrix

| Gate | Result | Evidence |
|---|---:|---|
| Domain focused | PASS | 29/29 |
| Critical HTTP focused | PASS | 5/5 |
| Backend restore | PASS | all projects up to date |
| Backend Release build | PASS | 0 errors; 17 warnings reported in the final incremental build |
| Backend full tests | PASS | Domain 29 + Application 108 + API 84 = 221 |
| Frontend clean `npm ci` | PASS | 490 packages installed |
| Frontend production build | PASS | 845.11 kB initial bundle; existing budget warning remains |
| Frontend full tests | PASS | 42/42 across 10 files |
| Diff whitespace check | PASS | `git diff --check` / cached checks |

The clean install still reports 30 audit findings: 3 low, 5 moderate, 21 high, and 1 critical. No broad audit fix or dependency upgrade was attempted.

### Integration coverage

- Auth: Secure/HttpOnly/SameSite=None access cookie, cookie-accepted protected endpoint, anonymous 401, wrong-role 403, refresh rotation.
- Commerce: real HTTP cart addition and checkout, persisted order, stock deduction, cart clearing, insufficient-stock rejection, cross-user order denial.
- Product roles: admin HTTP create/update/delete, seller ownership denial, structured representative validation 400.
- Order lifecycle: valid transition, invalid transition, non-admin rejection through HTTP.
- Frontend: header role/badges/mobile state, cart quantity/remove and in-flight guard, checkout invalid/success/error, product-card cart/wishlist event handling.

### Remaining risks

- Refresh token remains readable in localStorage/sessionStorage; later auth hardening should consider an HttpOnly refresh cookie.
- Nullable warnings remain; they were not broadly chased.
- Initial Angular bundle exceeds the configured budget by 345.11 kB.
- npm audit reports 30 findings; direct exploitable high/critical risks need later targeted triage.
- Coverage is intentionally focused, not comprehensive.
- Broad README portfolio/architecture claims remain for the Phase 6 audit.

### Over-engineering audit

- Acceptance criterion: tests explicitly required by `TEST_STRATEGY.md` before UI work.
- Simpler option used: existing xUnit, EF InMemory, `WebApplicationFactory`, Angular TestBed, and Vitest; no new framework/package/harness project.
- Measurable failures prevented: refresh query translation failure, invalid state transitions, cookie/role regressions, checkout persistence/ownership regressions, and component behavior loss during template changes.

No abstraction, service, library, queue, cache, architecture layer, E2E framework, or broad dependency change was added.

### Documentation updates

- Added the repository execution contract and test strategy to source control.
- Updated `README.md` and `SYSTEM_STATE.md` for cookie auth and CI-only validation.
- Updated `docs/CURRENT_STATE.md` and `docs/PROJECT_EXECUTION_PLAN.md` to mark Phase 2.5 complete and Phase 3 not started.
- Created this canonical `docs/PHASE2_5_REPORT.md`.

### Git commits

- `aa4f4a3` — `chore: add portable line ending policy`
- `5f476c1` — `fix: restore refresh token rotation`
- `c1e7bf2` — `test: protect critical commerce flows`
- `a856e31` — `docs: correct auth and CI claims`
- Final handoff documentation commit contains the supplied contract/strategy, current state, execution plan, and this report.

### GitHub push status

`recovery/ui-portfolio` is pushed to `origin`; the exact verified remote SHA is printed in the completion handoff. No force-push or `main` rewrite was used.

### Tag status

Annotated tag `v0.1.0-green-baseline` is pushed after the full stable-tag gate. Its remote target is verified in the completion handoff.

### Current state

Phase 2.5 is complete: portable, green, test-protected at selected critical boundaries, accurately documented, committed, backed up, tagged, and snapshotted. Phase 3 has not started.

### Remaining work

- Phase 3: application shell and home UI recovery.
- Phase 4: customer commerce UI flows.
- Phase 5: seller/admin UI normalization.
- Phase 6: portfolio/security/dependency/bundle hardening and claim audit.

### Next phase

Phase 3 may begin only after explicit user direction. Its first vertical slice is the shared shell, responsive header/footer, minimal Light/Dark tokens, and API-driven home page while preserving the Phase 2.5 behavior tests.

### STOP / handoff

- Requested: Phase 2.5 only.
- Completed: portability, accurate auth/CI docs, critical Domain/API/frontend tests, refresh-token fix, green gate, logical commits, branch backup, stable tag, and lightweight external ZIP.
- Exact files changed: listed by feature above and in Git commits.
- Tests/builds: all gates in the test matrix pass.
- Remaining: Phase 3 onward; none started.
- Blockers/risks: listed above; none blocks the green baseline.
- Next exact action: wait for explicit approval, then start the Phase 3 shell/home vertical slice.
- User input needed: explicit authorization to start Phase 3.
- Remote push/tag status: pushed and verified; exact SHAs plus ZIP path/size/SHA-256 are printed in the completion handoff.
