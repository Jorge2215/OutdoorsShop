@description('Environment name (e.g. dev, prod)')
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

@description('Principal ID of the App Service system-assigned managed identity')
param appServicePrincipalId string

@description('Principal ID of the Functions App system-assigned managed identity')
param functionsPrincipalId string

@description('SQL admin password — stored as a Key Vault secret')
@secure()
param sqlAdminPassword string

@description('JWT signing secret — stored as a Key Vault secret')
@secure()
param jwtSecret string

@description('Full SQL connection string — stored as a Key Vault secret')
@secure()
param sqlConnectionString string

@description('Storage account connection string — stored as a Key Vault secret')
@secure()
param storageConnectionString string

var keyVaultName = 'kv-outdoors-${environmentName}'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    accessPolicies: [
      // App Service — read secrets for connection strings and JWT key
      {
        tenantId: subscription().tenantId
        objectId: appServicePrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
      // Functions App — read secrets for DB and storage connections
      {
        tenantId: subscription().tenantId
        objectId: functionsPrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

resource sqlAdminPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'sql-admin-password'
  parent: keyVault
  properties: {
    value: sqlAdminPassword
  }
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'sql-connection-string'
  parent: keyVault
  properties: {
    value: sqlConnectionString
  }
}

resource jwtSecretResource 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'jwt-secret'
  parent: keyVault
  properties: {
    value: jwtSecret
  }
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'storage-connection-string'
  parent: keyVault
  properties: {
    value: storageConnectionString
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
