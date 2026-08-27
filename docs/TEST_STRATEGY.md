# Test Strategy — High Value, Not Maximum Count

## Principle
Tests protect important behavior and enable safe UI/refactor work.
The target is NOT 100% coverage.

Use the cheapest test layer that gives confidence:
1. domain/unit test for pure rules;
2. service test for application behavior;
3. API integration test for HTTP/auth/persistence boundaries;
4. frontend component/service test for client behavior;
5. full E2E only if a critical cross-stack flow cannot be protected otherwise.

## Current known state
- Application tests: 108 passing.
- API tests: 79 passing.
- Domain test project: currently no discovered tests.
- Frontend: 33 passing tests across 6 spec files.
- Green does NOT mean coverage is sufficient.

## Phase 2.5 required test backlog

### A. Domain invariants — MUST ADD
Add focused tests for existing rules, especially:
- Order status transition valid/invalid paths.
- Product discount/price invariants, including nullable CompareAtPrice edge cases.
- Stock quantity cannot transition into invalid state if domain methods enforce it.
- Review/rating invariants if implemented in Domain.
- Payment/order status invariants if domain-owned.

Do not create tests for getters/setters or trivial constructors.

### B. API integration — MUST STRENGTHEN
Use the existing integration harness.

Critical scenarios:
1. Auth
   - successful login sets expected HttpOnly access cookie;
   - protected endpoint accepts cookie auth;
   - unauthenticated protected endpoint returns 401;
   - role-protected endpoint returns 403 for wrong role;
   - refresh behavior has at least one integration test.

2. Cart → Checkout → Order
   - add real product to cart;
   - checkout creates order;
   - stock changes correctly;
   - cart is cleared;
   - insufficient stock prevents checkout;
   - unauthorized user cannot read another user's order.

3. Product/Admin/Seller
   - Admin create/update/delete happy path through HTTP;
   - Seller cannot mutate another seller's product;
   - validation returns structured 400 for a representative invalid request.

4. Order status
   - valid transition succeeds;
   - invalid transition is rejected;
   - unauthorized role cannot update status.

Do not duplicate the same rule at every layer unless the boundary itself is what is being tested.

### C. Frontend protection before UI recovery — MUST ADD MINIMALLY
Protect behavior likely to break during HTML/SCSS/template restructuring:
- Header role-aware navigation and cart/wishlist badges.
- Mobile menu open/close behavior.
- Cart quantity/remove behavior.
- Checkout invalid form blocks submit; valid submit handles success/error.
- Product-card wishlist/cart event behavior if template restructuring touches it.
- Home async loading/error/empty state only if home logic is changed.

Do NOT snapshot huge HTML trees.
Do NOT test CSS implementation details.

### D. Optional E2E
Do not introduce Playwright/Cypress now unless Phase 3 exposes a gap that cannot be protected reasonably with existing tests.

If introduced later, keep only 3–5 smoke flows:
- login;
- browse → cart;
- checkout;
- admin order status;
- seller product management.

## Test acceptance per feature
Before commit:
- focused changed tests pass;
- full relevant project tests pass;
- production build passes.

Before stable tag:
- clean frontend `npm ci`;
- frontend production build + all tests;
- backend restore/build + all discovered tests;
- critical integration suite passes.

## Reporting
Never say “coverage is strong” based only on pass counts.
Report:
- discovered test count;
- layers covered;
- critical flows covered;
- known gaps.
