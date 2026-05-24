// OutdoorsShop — dev environment parameters
// Usage:
//   az deployment group create \
//     --resource-group rg-outdoors-dev \
//     --template-file infra/main.bicep \
//     --parameters infra/parameters/dev.bicepparam \
//     --parameters sqlAdminPassword='<password>' jwtSecret='<secret>'
//
// IMPORTANT: sqlAdminPassword and jwtSecret are @secure() parameters.
// Do NOT add them to this file. Pass them via CLI flags or Azure Key Vault
// parameter file injection in your CI/CD pipeline.

using '../main.bicep'

param environmentName = 'dev'
param location = 'eastus'
param sqlAdminLogin = 'sqladmin'
param frontendUrl = 'http://localhost:3000'
param jwtAudience = 'OutdoorsShopClient'
