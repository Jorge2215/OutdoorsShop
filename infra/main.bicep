// OutdoorsShop — Azure Infrastructure Orchestrator
// Deploys all resources for one environment (dev or prod).
//
// Deployment order (Bicep resolves implicit dependencies via output references):
//   1. monitoring   — App Insights + Log Analytics (no upstream deps)
//   2. storage      — Storage Account + containers (no upstream deps)
//   3. sql          — SQL Server + Database (no upstream deps)
//   4. appservice   — App Service Plan + Web App (needs monitoring outputs)
//   5. functions    — Functions App (needs monitoring + storage outputs)
//   6. keyvault     — Key Vault + secrets + access policies
//                     (needs principalIds from steps 4 & 5, connection strings from 2 & 3)
//
// NOTE: App Service and Functions app settings contain Key Vault reference strings.
//       Those references are resolved at runtime by Azure after Key Vault (step 6) is deployed.
//       On first deployment the apps will be temporarily unhealthy until step 6 completes — this
//       is expected behaviour for a single-pass IaC bootstrap.

targetScope = 'resourceGroup'

// ---------------------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------------------

@description('Environment name appended to all resource names (e.g. dev, prod)')
param environmentName string = 'dev'

@description('Azure region for all resources')
param location string = 'eastus'

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@description('Whether to provision a new Azure SQL Server + Database')
param deploySql bool = true

@description('Existing SQL Server FQDN to use when deploySql is false')
param existingSqlServerFqdn string = ''

@description('SQL Server administrator password — never store in source control')
@secure()
param sqlAdminPassword string

@description('Existing SQL connection string to store in Key Vault when deploySql is false')
@secure()
param existingSqlConnectionString string = ''

@description('JWT signing secret — never store in source control')
@secure()
param jwtSecret string

@description('Frontend application origin URL (used for CORS and JWT issuer)')
param frontendUrl string = 'http://localhost:3000'

@description('JWT audience claim value')
param jwtAudience string = 'OutdoorsShopClient'

// ---------------------------------------------------------------------------
// Derived values
// ---------------------------------------------------------------------------

var apiBaseUrl = 'https://app-outdoors-api-${environmentName}.azurewebsites.net'

// ---------------------------------------------------------------------------
// Module: Application Insights + Log Analytics
// ---------------------------------------------------------------------------

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    environmentName: environmentName
    location: location
  }
}

// ---------------------------------------------------------------------------
// Module: Storage Account + Blob containers
// ---------------------------------------------------------------------------

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    environmentName: environmentName
    location: location
  }
}

// ---------------------------------------------------------------------------
// Module: Azure SQL Server + Database
// ---------------------------------------------------------------------------

module sql 'modules/sql.bicep' = if (deploySql) {
  name: 'sql'
  params: {
    environmentName: environmentName
    location: location
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
  }
}

var resolvedSqlServerFqdn = deploySql
  ? sql!.outputs.sqlServerFqdn
  : existingSqlServerFqdn

// ---------------------------------------------------------------------------
// Module: App Service Plan + Web API
// ---------------------------------------------------------------------------

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    environmentName: environmentName
    location: location
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: 'kv-outdoors-${environmentName}'
    frontendUrl: frontendUrl
    jwtIssuer: apiBaseUrl
    jwtAudience: jwtAudience
  }
}

// ---------------------------------------------------------------------------
// Module: Azure Functions App (Consumption plan)
// ---------------------------------------------------------------------------

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    environmentName: environmentName
    location: location
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: 'kv-outdoors-${environmentName}'
  }
}

// ---------------------------------------------------------------------------
// Module: Key Vault + secrets + managed identity access policies
// Deployed last so it can consume principalIds from App Service and Functions.
// ---------------------------------------------------------------------------

module keyvaultWithNewSql 'modules/keyvault.bicep' = if (deploySql) {
  name: 'keyvault-new-sql'
  params: {
    environmentName: environmentName
    location: location
    appServicePrincipalId: appservice.outputs.principalId
    functionsPrincipalId: functions.outputs.principalId
    sqlAdminPassword: sqlAdminPassword
    jwtSecret: jwtSecret
    sqlConnectionString: sql.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
  }
}

module keyvaultWithExistingSql 'modules/keyvault.bicep' = if (!deploySql) {
  name: 'keyvault-existing-sql'
  params: {
    environmentName: environmentName
    location: location
    appServicePrincipalId: appservice.outputs.principalId
    functionsPrincipalId: functions.outputs.principalId
    sqlAdminPassword: sqlAdminPassword
    jwtSecret: jwtSecret
    sqlConnectionString: existingSqlConnectionString
    storageConnectionString: storage.outputs.connectionString
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('Web API public URL')
output apiUrl string = 'https://${appservice.outputs.defaultHostname}'

@description('Functions App host URL')
output functionsUrl string = 'https://${functions.outputs.defaultHostname}'

@description('Storage Account name (needed for blob URL construction)')
output storageAccountName string = storage.outputs.storageAccountName

@description('Blob storage public endpoint')
output blobEndpoint string = storage.outputs.blobEndpoint

@description('Key Vault URI')
output keyVaultUri string = deploySql
  ? keyvaultWithNewSql!.outputs.keyVaultUri
  : keyvaultWithExistingSql!.outputs.keyVaultUri

@description('SQL Server FQDN')
output sqlServerFqdn string = resolvedSqlServerFqdn
