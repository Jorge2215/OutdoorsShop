# ADR: Azure Bicep Infrastructure Templates for Dev Environment

**Date:** 2026-05-23  
**By:** Toru (Architect)  
**Status:** Accepted

## Context

The OutdoorsShop project needs reproducible, version-controlled Azure infrastructure for the dev environment. Manual portal configuration is not acceptable for a PoC that aims to demonstrate engineering best practices. All secrets must never appear in config files, environment variables, or deployment logs.

## Decision

Provision all dev Azure resources via Azure Bicep templates in `infra/`. A single orchestrator (`main.bicep`) calls six modules in dependency order. Sensitive parameters (`sqlAdminPassword`, `jwtSecret`) are `@secure()` and must be passed via CLI flags or CI/CD secret injection — never committed to source.

## Resources provisioned

| Resource | Name | SKU / Tier |
|---|---|---|
| Log Analytics | `law-outdoors-dev` | PerGB2018, 30-day retention |
| Application Insights | `appi-outdoors-dev` | Workspace-based |
| SQL Server | `sql-outdoors-dev` | v12, TLS 1.2, Azure services firewall open |
| SQL Database | `sqldb-outdoors-dev` | Basic (5 DTU), geo-redundant backup |
| Storage Account | `stoutdoorsdev` | Standard_LRS, StorageV2, HTTPS-only |
| App Service Plan | `asp-outdoors-dev` | B1, Linux |
| App Service (API) | `app-outdoors-api-dev` | .NET 10, system-assigned MI |
| Functions Hosting Plan | `asp-outdoors-func-dev` | Y1 (Consumption), Linux |
| Functions App | `func-outdoors-dev` | .NET isolated 10, system-assigned MI |
| Key Vault | `kv-outdoors-dev` | Standard, soft-delete 7 days |

## Secret management approach

- All secrets stored in Key Vault (`kv-outdoors-dev`).
- App Service and Functions access secrets via `@Microsoft.KeyVault(VaultName=...;SecretName=...)` app setting references.
- Managed identities granted `get`/`list` access policies on Key Vault.
- Bicep outputs for connection strings use `@secure()` to prevent logging.

## Deployment order rationale

Key Vault is deployed **last** in `main.bicep`. This is intentional: the Key Vault access policies require the `principalId` from the App Service and Functions managed identities, which are only available after those resources are created. Bicep resolves this via output references, creating an implicit dependency chain. App settings with Key Vault references are valid strings from the moment of App Service creation; Azure resolves them at runtime once Key Vault is live.

## DB migration note (ShopAdmin)

The `ShopAdmin` SQL user requires the `db_ddladmin` role so that EF Core's migration runner can execute `CREATE`/`ALTER`/`DROP` DDL statements. Without this role, `dotnet ef database update` fails. This is a one-time manual step documented in `infra/README.md`.

## Consequences

- Single `az deployment group create` command deploys all dev infrastructure.
- No secrets in source code, app settings, or CI logs.
- Key Vault access policies (not RBAC) are used as specified; can be migrated to RBAC in a future ADR.
- `db_ddladmin` must be granted manually after first SQL deployment (not automated in Bicep to avoid embedding the app user password in the template).
