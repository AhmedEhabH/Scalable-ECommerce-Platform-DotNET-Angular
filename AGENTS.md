# AGENTS.md — Execution Contract

## Mandatory identity
At the beginning of EVERY work session and EVERY final/stop report, print:
- Agent/tool name and version
- Exact model name
- Reasoning/effort level if the tool exposes it
- Current branch
- Current HEAD

Never report a generic model family if the CLI shows a more exact model.

## North-star goal
Deliver a polished, reliable, demonstrable full-stack e-commerce portfolio project using .NET 8 + Angular 21.
The project must show engineering judgment, not technology accumulation.

Immediate priorities, in order:
1. Keep builds/tests green.
2. Preserve working business features.
3. Improve high-value tests for critical user flows.
4. Recover the UI into a coherent professional storefront/admin experience.
5. Keep documentation accurate.
6. Keep work backed up on GitHub.
7. Remove or justify over-engineered claims/technology only when it directly improves the portfolio story.

Do NOT change this goal without explicit user approval.

## Anti-over-engineering rule
Before adding any abstraction, service, library, pattern, framework, queue, cache, layer, test harness, or new artifact, answer:
1. Which current acceptance criterion requires it?
2. What simpler option was considered?
3. What measurable failure does it prevent?
If these cannot be answered in 3 short bullets, do NOT add it.

Prefer completing one feature vertically over touching many areas partially.

## Work-unit rule
A work unit must be small enough to:
- implement,
- test,
- document,
- review,
- commit,
- and push
as one coherent feature/fix.

Do not leave a feature half-implemented when a reasonable complete slice can be finished in the same session.

## Mandatory reading order before changes
1. `AGENTS.md`
2. `docs/CURRENT_STATE.md`
3. `docs/PROJECT_EXECUTION_PLAN.md`
4. `docs/TEST_STRATEGY.md`
5. latest `docs/PHASE*_REPORT.md` or `docs/CODEX_PHASE*_REPORT.md`

Do not re-discover facts already documented unless validation is required.

## Naming conventions
Use these commit prefixes only:
- `fix:`
- `feat:`
- `test:`
- `refactor:`
- `docs:`
- `chore:`
- `ci:`
- `perf:`

Branches:
- `recovery/<short-purpose>`
- `feat/<short-purpose>`
- `fix/<short-purpose>`

Stable tags:
- `v0.x.y-<short-name>`
Example: `v0.1.0-green-baseline`

Reports:
- `docs/PHASE<N>_REPORT.md`
- Never create competing names for the same phase.

## Test-first execution
Before editing a behavior:
1. identify existing tests;
2. identify affected dependencies;
3. define acceptance cases;
4. add/update the smallest meaningful test set;
5. implement;
6. run focused tests;
7. run full relevant test suite;
8. run build.

Do not write large code changes first and discover compilation constraints afterward.

## Dependency-impact check
Before changing a feature, list:
- entry point(s)
- service(s)
- model/DTO(s)
- backend endpoint(s)
- persistence impact
- shared state
- auth/role impact
- UI routes/components
- tests
- docs

Then touch only the required files.

## Documentation is part of Definition of Done
Every completed work unit must update, when applicable:
- `docs/CURRENT_STATE.md`
- `docs/PROJECT_EXECUTION_PLAN.md`
- README only when public-facing facts change
- phase report

Never allow documentation to claim functionality that the code/tests do not prove.

## Git/GitHub rule
Do not trap work on one machine.
For each completed stable work unit:
1. validate;
2. commit;
3. push the branch to `origin`;
4. confirm remote branch/commit;
5. if it is a stable milestone, create an annotated tag and push the tag.

Never rewrite `main` history.
Never force-push unless explicitly approved.

## Stable milestone tag gate
Tag only when:
- backend build passes;
- backend discovered tests pass;
- frontend clean install/build/tests pass;
- no known critical regression;
- documentation is current;
- working tree is clean.

## Stop / interruption protocol
If stopping for ANY reason, print and write a report containing:
1. exact model/tool identity;
2. current branch and HEAD;
3. what was requested;
4. what was completed;
5. exact files changed;
6. tests/builds run and results;
7. what remains;
8. blockers/risks;
9. next exact action;
10. what input/approval is needed from the user;
11. remote push/tag status.

Never stop with only “I need X”.

## Phase-end report format
Every phase report must contain:

### Identity
### Goal
### Starting state
### Scope promised
### Changes by feature
For each feature:
- problem
- root cause
- files changed
- behavior before
- behavior after
- tests added/updated
- verification result

### Test matrix
### Integration coverage
### Remaining risks
### Over-engineering audit
### Documentation updates
### Git commits
### GitHub push status
### Tag status
### Current state
### Remaining work
### Next phase
### STOP / handoff

## Lightweight project ZIP
At every stable phase, create OUTSIDE the project folder:

`project-YYYY-MM-DD-HHMM.zip`

The archive must be lightweight and exclude at least:
- `.git`
- `node_modules`
- `bin`
- `obj`
- `.angular`
- `dist`
- `coverage`
- logs
- caches
- secrets / `.env`

Include source, tests, docs, lockfiles, migrations, CI, Docker files, and `.env.example`.

Print:
- full ZIP path
- filename
- size
- SHA-256

The ZIP is a handoff snapshot; GitHub remains the canonical source.

## Productivity rule
Do not spend a long cycle on speculative analysis.
If the next step is clear and safe, execute it.
If a decision is reversible and low-risk, choose the simplest option and document it.
Ask the user only for decisions that materially change scope or product behavior.
