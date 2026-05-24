@description('Environment name (e.g. dev, prod)')
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault name — used to build Key Vault reference strings for app settings')
param keyVaultName string

@description('Storage account name used by the Functions host and deployment package container')
param storageAccountName string

var functionsHostingPlanName = 'asp-outdoors-func-flex-${environmentName}'
var functionAppName = 'func-outdoors-${environmentName}'
var deploymentContainerName = 'function-releases'
var kvRef = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName='

resource functionsHostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: functionsHostingPlanName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
    family: 'FC'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsHostingPlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: 'SystemAssigned'
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}/${deploymentContainerName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
      scaleAndConcurrency: {
        instanceMemoryMB: 2048
        maximumInstanceCount: 100
        alwaysReady: []
      }
    }
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: '${kvRef}storage-connection-string)'
        }
        {
          name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
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
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '${kvRef}sql-connection-string)'
        }
        {
          name: 'Azure__Storage__ConnectionString'
          value: '${kvRef}storage-connection-string)'
        }
        {
          name: 'Azure__Storage__OrderReceiptsContainer'
          value: 'order-receipts'
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output defaultHostname string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
