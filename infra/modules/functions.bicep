@description('Environment name (e.g. dev, prod)')
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault name — used to build Key Vault reference strings for app settings')
param keyVaultName string

var functionsHostingPlanName = 'asp-outdoors-func-${environmentName}'
var functionAppName = 'func-outdoors-${environmentName}'

// Consumption plan for Azure Functions (Y1 / Dynamic — scale to zero)
resource functionsHostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: functionsHostingPlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Required for Linux
  }
}

var kvRef = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName='

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsHostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10'
      appSettings: [
        // Functions host runtime
        {
          name: 'AzureWebJobsStorage'
          value: '${kvRef}storage-connection-string)'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        // Application Insights
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        // Database — resolved from Key Vault
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '${kvRef}sql-connection-string)'
        }
        // Storage — resolved from Key Vault
        {
          name: 'Azure__Storage__ConnectionString'
          value: '${kvRef}storage-connection-string)'
        }
        {
          name: 'Azure__Storage__OrderReceiptsContainer'
          value: 'order-receipts'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output defaultHostname string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
