# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| System architecture, ADR, Azure topology, deployment strategy | Toru | Infra design, solution structure, cross-cutting API contracts |
| .NET 10, C#, ASP.NET Core, Web API, Entity Framework Core, Azure SQL, database schema, migrations | Cinnamon | CRUD endpoints, EF migrations, repository pattern, service layer |
| Azure Functions (seasonal discounts, payment confirmation, stock updates) | Cinnamon (primary) + Toru (architecture) | Function triggers, bindings, business logic |
| Azure Blob Storage (product images, order receipts, CSV/Excel exports) | Cinnamon | Blob upload/download, SAS tokens, report generation |
| JWT auth, ASP.NET Core Identity, RBAC, backend roles | Cinnamon | Auth middleware, role policies, token generation |
| React, TypeScript, UI components, product catalog, categories (Camping/Trekking/Cycling/Climbing) | Malta | Component design, state management, API integration |
| Shopping cart, order flow UI, payment simulation UI | Malta | Cart state, order review, checkout flow |
| Login/register UI, role-based views (Admin vs Customer) | Malta | Auth forms, session management, protected routes |
| Unit tests (.NET xUnit), integration tests (WebApplicationFactory), API contract tests | Creta | Backend test coverage |
| Frontend component tests (Vitest, React Testing Library), E2E tests (Playwright) | Creta | Frontend test coverage |
| Test plan, test strategy, edge cases, QA review | Creta | Before feature is marked done |
| Documentation, ADR, session log, changelog, decisions | Scribe | Silent — automatic |
| GitHub issues, backlog, PR status, CI/CD, work queue | Ralph | Monitoring loop |
| "Team" or multi-domain requests | Toru + Cinnamon + Malta + Creta | Parallel fan-out |
| Code review | Toru | Architecture and API contracts |
| Testing | Creta | All test layers |
| Scope & priorities | Toru | Architectural decisions, trade-offs |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.

## Work Type → Agent

| Work Type | Primary | Secondary |
|-----------|---------|----------|
| System architecture, deployment strategy | Toru | — |
| .NET 10 APIs, database integration | Cinnamon | — |
| React UI, shopping flows | Malta | — |
| Validation, edge cases, error handling | Creta | — |

