# OutdoorsShop — Azure Infrastructure (Bicep)

This directory contains the complete Azure infrastructure-as-code for the OutdoorsShop application, written in Azure Bicep.

## Directory structure

```
infra/
  main.bicep              — Orchestrator; calls all modules in dependency order
  parameters/
    dev.bicepparam        — Dev environment non-sensitive parameters
  modules/
    monitoring.bicep      — Application Insights + Log Analytics workspace
    sql.bicep             — Azure SQL Server + Database (Basic tier for dev)
    storage.bicep         — Storage Account + blob containers
    appservice.bicep      — App Service Plan (B1/Linux) + Web API
    functions.bicep       — Functions App (Consumption/Linux) + hosting plan
    keyvault.bicep        — Key Vault + secrets + managed identity access policies
```

## Resources provisioned (dev)

| Resource | Name | Notes |
|---|---|---|
| Resource Group | `rg-outdoors-dev` | Create manually before first deploy |
| Log Analytics | `law-outdoors-dev` | Backing workspace for App Insights |
| Application Insights | `appi-outdoors-dev` | Linked to App Service + Functions |
| SQL Server | `sql-outdoors-dev` | .NET 10 / EF Core migrations target |
| SQL Database | `sqldb-outdoors-dev` | Basic (5 DTU), geo-redundant backup |
| Storage Account | `stoutdoorsdev` | LRS, StorageV2 |
| → Blob container | `product-images` | Public blob — product catalog images |
| → Blob container | `order-receipts` | Private — SAS-token access |
| → Blob container | `reports` | Private — CSV/Excel exports, SAS-token access |
| App Service Plan | `asp-outdoors-dev` | B1, Linux |
| App Service (API) | `app-outdoors-api-dev` | .NET 10, system-assigned managed identity |
| Functions Hosting Plan | `asp-outdoors-func-dev` | Consumption Y1, Linux |
| Functions App | `func-outdoors-dev` | .NET isolated 10, system-assigned managed identity |
| Key Vault | `kv-outdoors-dev` | Standard, soft-delete 7 days |

## Secrets stored in Key Vault

| Secret name | Content |
|---|---|
| `sql-admin-password` | SQL Server admin password |
| `sql-connection-string` | Full ADO.NET connection string for App Service + EF Core |
| `jwt-secret` | JWT signing secret (HMAC-SHA256) |
| `storage-connection-string` | Storage Account connection string for SDK + Functions host |

## Prerequisites

1. **Azure CLI** installed and authenticated (`az login`).
2. **Bicep CLI** installed (`az bicep install`).
3. The target resource group already exists:

   ```bash
   az group create --name rg-outdoors-dev --location eastus
   ```

## Deploy (dev environment)

```bash
az deployment group create \
  --resource-group rg-outdoors-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters sqlAdminPassword='<strong-password>' jwtSecret='<random-256-bit-secret>'
```

> **Never** commit `sqlAdminPassword` or `jwtSecret` to source control.  
> In CI/CD, inject them from GitHub Actions secrets via `--parameters sqlAdminPassword='${{ secrets.SQL_ADMIN_PASSWORD }}'`.

## Validate without deploying (what-if)

```bash
az deployment group what-if \
  --resource-group rg-outdoors-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters sqlAdminPassword='<password>' jwtSecret='<secret>'
```

## Post-deployment: EF Core migrations

After the SQL Server and database are created, run EF Core migrations from your local machine or CI pipeline:

```bash
cd src/OutdoorsShop.Api
dotnet ef database update \
  --connection "Server=tcp:sql-outdoors-dev.database.windows.net,1433;Initial Catalog=sqldb-outdoors-dev;User ID=sqladmin;Password=<password>;Encrypt=True;"
```

### ShopAdmin DB role requirement

The application database user (`ShopAdmin`) must have the **`db_ddladmin`** role on `sqldb-outdoors-dev` for EF Core migrations to apply schema changes at runtime. Grant this role once after the database is created:

```sql
-- Run against sqldb-outdoors-dev as the SQL admin
CREATE USER [ShopAdmin] WITH PASSWORD = '<app-user-password>';
ALTER ROLE db_datareader ADD MEMBER [ShopAdmin];
ALTER ROLE db_datawriter ADD MEMBER [ShopAdmin];
ALTER ROLE db_ddladmin   ADD MEMBER [ShopAdmin];
```

> `db_ddladmin` is required so EF Core's migration runner can `CREATE`/`ALTER`/`DROP` tables and indexes.  
> Without it, `dotnet ef database update` will fail with _"The user does not have permission to perform this action"_.

## Architecture notes

- **Managed identities** — both App Service (`app-outdoors-api-dev`) and Functions (`func-outdoors-dev`) use system-assigned managed identities with `get`/`list` access to Key Vault secrets.  
- **No secrets in app settings** — all sensitive values are stored in Key Vault and referenced from app settings using `@Microsoft.KeyVault(VaultName=...;SecretName=...)` references, resolved transparently by the Azure App Service runtime.
- **Key Vault deployment order** — Key Vault is the last module deployed so it can receive the managed identity `principalId` values from App Service and Functions and set access policies correctly in a single pass.
- **CORS** — the Web API allows the `frontendUrl` origin with credentials. Update `frontendUrl` in `dev.bicepparam` if you deploy the frontend to a static web app.

## Resource naming convention

Pattern: `{abbreviation}-outdoors-{environment}`  
Storage accounts omit hyphens (Azure restriction): `stoutdoors{environment}`

Abbreviations follow [Microsoft CAF](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations):
`app`, `asp`, `sql`, `sqldb`, `st`, `func`, `kv`, `appi`, `law`, `rg`
