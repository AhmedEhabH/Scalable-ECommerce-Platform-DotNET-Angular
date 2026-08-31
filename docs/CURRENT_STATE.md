# Current State — Handoff Source of Truth

Date: 2026-08-27

## Agent-reviewed baseline
Branch: `recovery/ui-portfolio`

Stable baseline tag: `v0.1.0-green-baseline`

Phase 2 commits:
- `e1c5709` — pin .NET SDK and Angular dependency consistency
- `54e29fd` — complete HttpOnly-cookie access-token migration for ReviewService/tests
- `4511e25` — stabilize frontend/API test hosts
- `7fcd978` — validate frontend and backend in CI
- `f47e2a8` — Phase 2 report

Phase 2.5 commits:
- `aa4f4a3` — portable line-ending policy
- `5f476c1` — refresh-token rotation fix and critical HTTP flows
- `c1e7bf2` — domain and frontend critical-flow protection
- `a856e31` — accurate auth/CI documentation
- Phase 2.5 handoff documentation is committed after the entries above.

Phase 3 commits:
- (to be added after completion)

Baseline before Phase 2:
- `0fcf559`

## What is confirmed complete
- .NET projects stay targeting .NET 8.
- root `global.json` selects SDK 8.0.423.
- Angular framework/CDK/compiler packages are aligned at 21.2.13.
- normal clean `npm ci` was reported passing.
- frontend production build was reported passing.
- frontend discovered tests: 42/42 passing.
- backend discovered tests: Application 108 + API 84 + Domain 29 = 221 passing.
- access token is no longer manually read by ReviewService.
- CI has backend and frontend jobs.
- `.gitattributes` now normalizes repository text to LF, preserves Windows launchers as CRLF, and marks common assets binary.
- Domain tests: 29/29 passing for order, product, review, and payment invariants.
- API tests: 84/84 passing, including 5 HTTP scenarios covering cookie auth, refresh, checkout persistence/ownership, product roles/validation, and order status authorization.
- Frontend tests: 42/42 passing across 10 spec files, including header, cart, checkout, and product-card protection.
- Backend total: 221/221 discovered tests passing.
- The refresh-token query now uses persisted `ExpiresAt`/`RevokedAt` fields and succeeds through HTTP.
- README and `SYSTEM_STATE.md` describe HttpOnly access-cookie authentication and build/test CI accurately.
- Phase 3 UI recovery: Application shell, header, footer, design tokens (Light/Dark), and home page storefront completed.
- Phase 3.1 closeout: complete — footer fake links removed, Phase 3 screenshots committed, portable ZIP created, tag verified.

## Independent audit findings
1. Pass counts still do not imply broad coverage; the new tests protect only the explicitly selected critical behaviors.
2. Refresh token remains in localStorage/sessionStorage and is a security-hardening item; do not let it derail UI recovery unless auth is otherwise unsafe.
3. Backend nullable warnings remain; fix only proven runtime-risk items, not all warnings.
4. Initial Angular bundle remains above budget: 843.41 kB versus 500.00 kB.
5. Clean npm install reports 30 audit findings (3 low, 5 moderate, 21 high, 1 critical); triage direct exploitable risks later without broad upgrade churn.
6. README still uses some broad portfolio claims such as production-ready/full CQRS; Phase 6 owns that audit.

## GitHub portability status
Phase 2.5 completion requires `recovery/ui-portfolio` to be pushed and its remote SHA verified against local HEAD. The exact verified SHA is printed in the Phase 2.5 completion handoff.

## Stable tag status
The first stable tag is `v0.1.0-green-baseline`, created only after the complete green gate and documentation commit.

## Immediate next actions
1. Begin Phase 4 only when explicitly requested.
2. Implement customer commerce flows: product list/filter/sort, product details, wishlist, cart, checkout, order success/history.
3. Preserve the Phase 2.5/3 auth/cart/wishlist behavior protected by the tests.

## Current phase status
- Phase 1 forensic audit: COMPLETE
- Phase 2 green technical baseline: COMPLETE
- Phase 2.5 portability/test protection: COMPLETE
- Phase 3 UI recovery: COMPLETE
- Phase 4 customer flows: NOT STARTED
- Phase 5 seller/admin UI: NOT STARTED
- Phase 6 portfolio hardening: NOT STARTED

## North-star reminder
Do not add more architecture now.
The next visible value is:
GREEN + BACKED UP + TEST-PROTECTED → CUSTOMER COMMERCE FLOWS.
