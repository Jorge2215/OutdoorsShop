@description('Environment name (e.g. dev, prod)')
param environmentName string

@description('Azure region')
param location string = resourceGroup().location

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault name — used to build Key Vault reference strings for app settings')
param keyVaultName string

@description('Frontend origin URL for CORS (e.g. http://localhost:3000 or https://yourapp.azurestaticapps.net)')
param frontendUrl string = 'http://localhost:3000'

@description('JWT issuer claim value')
param jwtIssuer string

@description('JWT audience claim value')
param jwtAudience string = 'OutdoorsShopClient'

var appServicePlanName = 'asp-outdoors-${environmentName}'
var webAppName = 'app-outdoors-api-${environmentName}'

// Key Vault reference helper — resolves secret at App Service runtime via managed identity
var kvRef = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName='

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true // Required for Linux
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [
          frontendUrl
        ]
        supportCredentials: true
      }
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
        // SQL connection string — resolved from Key Vault at runtime
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '${kvRef}sql-connection-string)'
        }
        // JWT settings — secret resolved from Key Vault
        {
          name: 'JwtSettings__Secret'
          value: '${kvRef}jwt-secret)'
        }
        {
          name: 'JwtSettings__Issuer'
          value: jwtIssuer
        }
        {
          name: 'JwtSettings__Audience'
          value: jwtAudience
        }
        {
          name: 'JwtSettings__AccessTokenExpiryMinutes'
          value: '15'
        }
        {
          name: 'JwtSettings__RefreshTokenExpiryDays'
          value: '7'
        }
        // Blob storage connection — resolved from Key Vault
        {
          name: 'AzureStorage__ConnectionString'
          value: '${kvRef}storage-connection-string)'
        }
        {
          name: 'AzureStorage__ProductImagesContainer'
          value: 'product-images'
        }
        {
          name: 'AzureStorage__OrderReceiptsContainer'
          value: 'order-receipts'
        }
        {
          name: 'AzureStorage__ReportsContainer'
          value: 'reports'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output webAppName string = webApp.name
output defaultHostname string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
output appServicePlanId string = appServicePlan.id
