# Phase 3.1 Closeout — No Redesign

## Purpose

Close the small verification/handoff gaps found by the independent Phase 3 audit, then start Phase 4 immediately.

This is NOT a redesign phase and must not expand scope.

## Mandatory identity

At start and final report print:
- tool/version
- exact model if exposed
- reasoning/effort if exposed
- branch
- HEAD
- origin branch HEAD

## Fixed scope

### 1. Verify the Phase 3 tag correctly

Run and report both:

```bash
git rev-parse HEAD
git rev-parse origin/recovery/ui-portfolio
git rev-parse v0.2.0-ui-shell-home
git rev-parse v0.2.0-ui-shell-home^{}
git log -1 --format="%H %s" v0.2.0-ui-shell-home^{}
```

Do not confuse an annotated tag-object SHA with the commit SHA.

If the tag does not point to the intended green Phase 3 milestone, stop and report before changing the tag.

### 2. Add real Phase 3 screenshot evidence

The current handoff report claims screenshots but the portable snapshot contains an empty:

`docs/screenshots/phase3/`

Capture and COMMIT:

- `docs/screenshots/phase3/home-light-desktop.png` — 1440px
- `docs/screenshots/phase3/home-dark-desktop.png` — 1440px
- `docs/screenshots/phase3/home-mobile.png` — 390px
- `docs/screenshots/phase3/header-mobile-menu.png` — 390px, menu open

Use actual current Phase 3 UI, not historical screenshots.

If automated browser capture is unavailable, capture manually and wait only for that concrete evidence. Do not invent or claim screenshots.

### 3. Remove fake footer links

Audit the current footer.

Do not ship links such as:

- `href="#contact"`
- `href="#shipping"`
- `href="#returns"`
- `href="#faq"`
- `href="#privacy"`
- `href="#terms"`
- `href="#cookies"`
- `href="#twitter"`
- `href="#facebook"`
- `href="#instagram"`

unless those destinations actually exist.

Use the simplest solution:
- retain only real application routes / real external URLs;
- otherwise remove the fake item/section.

Do NOT create new support/legal/social pages in Phase 3.1.

Add a tiny footer component test only if behavior beyond static rendering needs protection. Do not over-test markup.

### 4. Produce the requested portable ZIP

Create outside the project exactly:

`project-YYYY-MM-DD-HHMM.zip`

It must contain the full lightweight project source and exclude:

- `.git`
- `node_modules`
- `bin`
- `obj`
- `.angular`
- `dist`
- `coverage`
- caches
- logs
- `.env`
- secrets

It MUST include:
- frontend source
- backend source
- tests
- docs
- screenshots
- package manifests/lockfile
- solution/projects
- migrations
- CI
- Docker files
- `.env.example`
- AGENTS.md

Verify the archive by listing it and confirming representative frontend/backend/test/doc files exist.

Print:
- full path
- filename
- number of files
- size
- SHA-256

The `.tar.gz` may remain as an optional extra, but the `.zip` is the required handoff artifact.

### 5. Documentation consistency

Update only what the closeout changes require:

- `docs/PHASE3_REPORT.md`
- `docs/CURRENT_STATE.md`

Correct screenshot wording so it matches committed evidence.
Record the verified peeled tag commit (`^{}`), not only the tag object SHA.

Do not rewrite the full README or execution plan unless a factual Phase 3 status is wrong.

## Validation

Run:

```bash
npm test -- --watch=false
npm run build
git diff --check
git status --short
```

Backend was untouched, so do NOT rerun all 221 backend tests unless a backend file changes.

If only footer/templates/docs/screenshots changed, frontend validation is sufficient.

## Git

Make one small closeout commit, for example:

`fix: close phase 3 verification gaps`

Push `recovery/ui-portfolio`.

Do NOT create a new semantic version tag just for this closeout.

If `v0.2.0-ui-shell-home` already points to the correct feature milestone, leave it immutable and document that the closeout commit follows it.

If it is genuinely wrong, STOP and report; do not silently retag a pushed release.

## Report

Create:

`docs/PHASE3_1_REPORT.md`

Include:

### Identity
### Audit findings addressed
### Exact files changed
### Footer link audit
### Screenshot evidence
### Validation results
### GitHub remote verification
### Tag object SHA vs peeled commit SHA
### ZIP path / file count / size / SHA-256
### Over-engineering check
### Current project state
### Remaining phases
### Next exact action
### STOP / handoff

Print the full report.

## Hard stop

Do NOT start Phase 4 in this same run.

This closeout should be short and mechanical.
