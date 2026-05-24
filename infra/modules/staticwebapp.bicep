// OutdoorsShop — Azure Static Web App module
// Provisions a Free-tier SWA for hosting the React SPA.
// Deployment token is retrieved via listSecrets() and returned as a secure output.

@description('Environment name appended to the resource name (e.g. dev, prod)')
param environmentName string

@description('Azure region for the Static Web App')
param location string = resourceGroup().location

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: 'app-outdoorsweb-${environmentName}'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

output staticWebAppName string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname

@secure()
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
