# Scribe — History

## Core Context

- **Project:** Outdoors Shop
- **Owner:** Jorgito
- **Role:** Session Logger / Documentation
- **Joined:** 2026-05-23
- **Repo:** https://github.com/Jorge2215/OutdoorsShop.git (dev = development, main = production)
- **Stack:** Markdown | .squad/ file system conventions
- **My scope:** Decision inbox merges, session logs, orchestration logs, history summarization, git commits of .squad/ state
- **Team:** Toru (Architect), Cinnamon (Backend), Malta (Frontend), Creta (Tester), Ralph (Monitor)
- **Purpose:** Proof of concept comparing GitHub Copilot + Squad vs traditional development

## Learnings

<!-- Append learnings below -->

### 2026-05-28T21:14:05.618-03:00 — Catalog MVP commit/push
- Only catalog MVP files (API, frontend, tests, .squad/skills) staged and committed
- Commit message details unified query, debounced React filters, and test coverage
- Push to `dev` succeeded; PR creation blocked by GitHub CLI auth (user must run `gh auth login`)

### 2026-05-27 — Azure Storage Queue & StockUpdate Function
- Order creation and admin inventory updates both enqueue `StockUpdateMessage` to `stock-updates` queue
- Function `StockUpdate` ([QueueTrigger("stock-updates")]) often no-ops due to prior DB/log update in API
- Function App runtime depends on `AzureWebJobsStorage` pointing to correct storage account


## 2026-05-27T19:51:34Z — Scribe

- Merged 2 inbox decisions related to async order receipts into `.squad/decisions/decisions.md` and removed the inbox files.
- Created orchestration log and session log entries; updated Cinnamon and Creta histories and archived Cinnamon's full history due to size.
- Staged and prepared `.squad/` artifacts for commit.


## 2026-05-27T20:47:27Z — scribe update
- Merged 1 inbox items into decisions.md
- Archived 0 entries (none older than cutoff)

## 2026-05-28T01:15:13Z — Scribe
- Pre-check: decisions.md was 78163 bytes and .squad/decisions/inbox/ contained 9 files.
- Merged 10 inbox decisions into decisions.md, removed the inbox files, dropped 1 duplicate residual inbox note, wrote orchestration/session logs, and summarized oversized history files.
- Health report: decisions.md finished at 98657 bytes; summarized histories: cinnamon.

## 2026-05-28T02:00:17Z — Scribe

- Recorded the AppDbContextFactory design-time configuration update in `.squad/decisions.md`.
- Logged that design-time EF now loads appsettings, environment-specific appsettings, API user secrets, and environment variables, with no silent local SQL fallback and a clear exception on missing `DefaultConnection`.
- Wrote matching session/orchestration entries for the validated build/tests and design-time EF info run.
