@description('Environment name (e.g. dev, prod)')
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

// Storage account names: no hyphens, max 24 chars, lowercase alphanumeric
var storageAccountName = 'stoutdoors${environmentName}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true // Required for the product-images public container
    allowSharedKeyAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-04-01' = {
  name: 'default'
  parent: storageAccount
}

// Public read — product images are served directly from Blob URL
resource productImagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  name: 'product-images'
  parent: blobService
  properties: {
    publicAccess: 'Blob'
  }
}

// Private — access via SAS tokens issued by the API
resource orderReceiptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  name: 'order-receipts'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// Private — CSV/Excel exports, access via SAS tokens
resource reportsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  name: 'reports'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// Private — Flex Consumption deployment packages
resource functionReleasesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  name: 'function-releases'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob

// Marked @secure() to prevent key material from appearing in deployment logs
@secure()
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
