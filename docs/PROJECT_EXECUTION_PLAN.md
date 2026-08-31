# Project Execution Plan

## Long-term goal
Produce a polished .NET 8 + Angular 21 multi-role e-commerce portfolio application that:
- builds reproducibly;
- has strong tests around critical business/user flows;
- has a coherent professional UI;
- demonstrates justified architectural decisions;
- can be cloned and continued on any machine;
- has accurate handoff documentation.

## Current milestone
Phase 3 UI recovery is complete. Phases 4A, 4B, 4C complete. Phases 4D, 4E, 4F, 5, 6 remain.

## Next shortest path to done

### Phase 2.5 — Protect and strengthen the baseline
Goal: finish only the test/documentation gaps that are dangerous before UI work.

Completed:
1. Added portable line-ending policy without mass source normalization.
2. Corrected HttpOnly-cookie authentication and CI documentation.
3. Added 29 focused Domain invariant tests.
4. Added 5 critical HTTP scenarios covering auth, checkout/order persistence, ownership, product roles/validation, and order status.
5. Added 9 frontend tests around header, cart, checkout, and product-card behavior.
6. Fixed the refresh-token EF query proven broken by the new HTTP boundary test.
7. Passed the complete backend/frontend green gate.
8. Committed the work logically and prepared the recovery branch, stable tag, and portable handoff snapshot.

Do NOT:
- chase 100% coverage;
- fix every nullable warning;
- add a new testing framework;
- add Playwright/Cypress unless existing component/integration tests cannot protect a critical UI flow;
- redesign UI yet.

Exit gate:
- backend green: 221/221 discovered tests;
- frontend clean install/build/tests green: 42/42 tests across 10 files;
- critical-flow tests listed in TEST_STRATEGY are present at the cheapest meaningful layer;
- docs accurate;
- branch and stable tag verified in the Phase 2.5 completion handoff.

### Phase 3 — UI recovery: application shell + home
Goal: visible result quickly.

Scope:
- design tokens: one branded Light/Dark pair;
- app container/layout;
- header desktop/mobile;
- footer;
- home page;
- shared button/input/card primitives only as needed by these screens.

Preserve:
- auth state;
- role navigation;
- wishlist/cart counts;
- notifications;
- theme persistence;
- API-driven homepage content.

Acceptance:
- desktop 1440px;
- tablet ~768px;
- mobile ~390px;
- no horizontal overflow;
- keyboard focus visible;
- Light/Dark both usable;
- build/tests green;
- screenshots captured.

Finish this phase fully before touching catalog details/admin styling.

Completed:
1. Design tokens refined to single branded Light/Dark pair (removed github/github-dark themes).
2. Application shell with sticky header, main content area, and footer.
3. Header: desktop navigation, mobile hamburger menu, cart/wishlist badges, theme toggle (Light/Dark), account menu with role-aware links.
4. Footer: brand, navigation groups (Shop, Support, Account, Legal), copyright, social links.
5. Home page: hero section, trust badges, featured products, categories, latest products - all API-driven.
6. Removed AI-generated visual excess: animated decorative shapes, glass effects, pulse animations, excessive gradients, duplicate promotional sections.
7. Responsive verified at 1440px, 768px, 390px breakpoints.
8. Accessibility: visible focus states, semantic HTML, reduced motion support, usable contrast.
8. All 121 frontend tests pass, 221 backend tests pass.

### Phase 4 — Customer commerce flows
Complete, in order:
1. product list/filter/sort — COMPLETE (Phase 4A: filter/sort/pagination/cards implementation + 14 tests; total frontend suite 58/58)
2. product card — COMPLETE (pre-existing + 2 tests in Phase 4A)
3. product details/reviews — COMPLETE (Phase 4B: 29 component tests added; implementation already existed in branch; total frontend suite 87/87 across 12 files)

Each route is a vertical slice:
UI + responsive + relevant tests + docs + screenshot + commit.

### Phase 5 — Seller/Admin
Normalize:
- admin shell;
- tables;
- forms;
- status badges;
- loading/error/empty states;
- seller products;
- analytics;
- admin orders/categories/products.

Do not add new features unless required to finish an existing flow.

### Phase 6 — Portfolio hardening
Only after functional/UI completion:
- correct README claims;
- architecture decision records for controversial choices;
- address high-risk security/dependency items;
- bundle optimization if still meaningful;
- Docker/runbook verification;
- final screenshots;
- final test matrix;
- final tag/release.

## Architecture scope guard
Do not expand CQRS merely to make the README true.
Do not keep RabbitMQ/Redis/Polly/RS256 merely to look advanced.
At Phase 6, either:
- demonstrate a concrete use case/test/measurement, or
- simplify/remove the claim/technology when low-risk.

## Progress reporting
Every report must show:

- Overall target
- Current phase
- Phase completion %
- What is finished
- What remains
- Blocking issue
- Next three concrete actions
- Estimated number of remaining phases (not time)

## Current progress

- Overall target: polished, reliable .NET 8 + Angular 21 portfolio storefront/admin application
- Current phase: Phase 3.1 complete; Phase 4 not started
- Phase completion: 100% (Phase 3.1 closeout done)
- Finished: portability policy, auth/CI docs, critical Domain/API/frontend tests, refresh-token fix, complete green gate, handoff preparation, UI recovery (shell, header, footer, tokens, home), Phase 3.1 closeout (footer links, screenshots, ZIP, report)
- Remaining: customer commerce UI, seller/admin UI, portfolio hardening
- Blocking issue: none in the codebase; Phase 4 awaits explicit user direction
- Next three actions: Phase 4 product list/filter/sort, product details, wishlist
- Estimated remaining phases: 3
