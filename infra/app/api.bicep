param name string
param location string = resourceGroup().location
param tags object = {}
param applicationInsightsName string = ''
param appSettings object = {}
param serviceName string = 'api'
param storageAccountName string
param deploymentStorageContainerName string
param virtualNetworkSubnetId string = ''
param instanceMemoryMB int = 2048
param maximumInstanceCount int = 100
param identityId string = ''
param identityClientId string = ''
param resourceToken string

param runtimeName string = 'dotnet-isolated'
param runtimeVersion string = '8.0'

@allowed(['SystemAssigned', 'UserAssigned'])
param identityType string = 'UserAssigned'

var abbrs = loadJsonContent('../abbreviations.json')
var applicationInsightsIdentity = 'ClientId=${identityClientId};Authorization=AAD'

// The application backend is a function app
module appServicePlan 'br/public:avm/res/web/serverfarm:0.5.0' = {
  name: 'appserviceplan'
  params: {
    name: '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    skuName: 'FC1'
    reserved: true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(applicationInsightsName)) {
  name: applicationInsightsName
}

module api 'br/public:avm/res/web/site:0.15.1' = {
  name: '${serviceName}-functions'
  params: {
    name: name
    location: location
    kind: 'functionapp,linux'
    tags: union(tags, { 'azd-service-name': serviceName })
    managedIdentities: {
      systemAssigned: identityType == 'SystemAssigned'
      userAssignedResourceIds: [
        '${identityId}'
      ]
    }
    serverFarmResourceId: appServicePlan.outputs.resourceId
    functionAppConfig: {
      location: location
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentStorageContainerName}'
          authentication: {
            type: identityType == 'SystemAssigned' ? 'SystemAssignedIdentity' : 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identityType == 'UserAssigned' ? identityId : '' 
          }
        }
      }
      scaleAndConcurrency: {
        instanceMemoryMB: instanceMemoryMB
        maximumInstanceCount: maximumInstanceCount
      }
      runtime: {
        name: runtimeName
        version: runtimeVersion
      }
    }
    appSettingsKeyValuePairs: union(appSettings,
      {
        AzureWebJobsStorage__blobServiceUri: storage.properties.primaryEndpoints.blob
        AzureWebJobsStorage__credential: 'managedidentity'
        AzureWebJobsStorage__clientId : identityClientId
        APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.?properties.ConnectionString
        APPLICATIONINSIGHTS_AUTHENTICATION_STRING: applicationInsightsIdentity
        AzureWebJobsFeatureFlags: 'EnableWorkerIndexing'
      })
    virtualNetworkSubnetId: !empty(virtualNetworkSubnetId) ? virtualNetworkSubnetId : null
    siteConfig: {
      alwaysOn: false
    }
  }
}

output SERVICE_API_NAME string = api.outputs.name
output SERVICE_API_IDENTITY_PRINCIPAL_ID string = identityType == 'SystemAssigned' ? api.outputs.?systemAssignedMIPrincipalId ?? '' : ''
