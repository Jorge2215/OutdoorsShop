---
date: 2026-05-31T21:53:53.116-03:00
agent: toru
---

# Main avatar rollout blocker

- Decision: keep the backend avatar rollout on main, but stop automated deployment attempts until AZURE_SQL_CONNECTION_STRING exists in the GitHub prod environment or repository secrets.
- What changed safely: main now contains the backend avatar API contract, additive Customers.AvatarPath / AvatarContentType migration, prod-targeted backend deploy settings, and an EF bundle build that no longer depends on a real design-time database connection.
- Evidence: workflow run 26730246749 (https://github.com/Jorge2215/OutdoorsShop/actions/runs/26730246749) passed build/test/package and failed exactly at Validate Azure deployment configuration with AZURE_SQL_CONNECTION_STRING empty.
- Next operator step: add the prod SQL connection string secret, then rerun ackend.yml on main; migration apply + App Service deploy should be able to proceed from the current head.
