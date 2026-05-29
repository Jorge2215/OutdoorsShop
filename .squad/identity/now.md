updated_at: 2026-05-28T23:12:35.244-03:00
focus_area: Backend deploy verified; live runtime check
active_issues:
  - Confirm live API route/health endpoint behavior after successful deploy
  - Resume auth/admin verification against the deployed backend
---

# Current Backlog & Sprint Planning (as of 2026-05-29T09:16:52.622-03:00)

## Recently Completed
- Catalog MVP: product price filters and sorting (PR #12)
- Fix merged product catalog filters on main (PR #13)
- Admin catalog auth fix (PR #15)
- Backend workflow and Azure deploy (runtime restore fix, CORS, CI/CD)
- Async Functions implementation and deploy

## Awaiting Validation
- Live API route/health endpoint behavior (user test)
- Auth/admin-path checks against deployed backend (user test)

## Ready for Next Sprint
- Resume catalog UI/UX improvements (pagination, error states, polish)
- Finalize and test async report export (end-to-end, including SAS download)
- Review and clean up CORS AllowedOrigins (remove old origins post-SWA migration)
- Automate backend API deploy in CI (extend backend.yml as per decision)
- Document and validate all new API endpoints in OpenAPI spec

## Blocked/Lower Priority
- Decommission old blob static website (after SWA fully verified)
- Advanced catalog features (slider, pagination envelope) — out of MVP scope

---
