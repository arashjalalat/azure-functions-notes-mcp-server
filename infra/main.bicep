targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
@allowed([ 'westeurope'])
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

param apiServiceName string = ''
param apiUserAssignedIdentityName string = ''
param applicationInsightsName string = ''
param logAnalyticsName string = ''
param resourceGroupName string = ''
param storageAccountName string = ''

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

var functionAppName = !empty(apiServiceName) ? apiServiceName : '${abbrs.webSitesFunctions}api-${resourceToken}'

var deploymentStorageContainerName = 'app-package-${take(functionAppName, 32)}-${take(toLower(uniqueString(functionAppName, resourceToken)), 7)}'

var storageAccountActualName = !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'

resource rg 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

module apiUserAssignedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.2' = {
  name: 'apiUserAssignedIdentity'
  scope: rg
  params: {
    location: location
    tags: tags
    name: !empty(apiUserAssignedIdentityName) ? apiUserAssignedIdentityName : '${abbrs.managedIdentityUserAssignedIdentities}api-${resourceToken}'
  }
}

module storage 'br/public:avm/res/storage/storage-account:0.27.1' = {
  name: 'storage'
  scope: rg
  params: {
    name: storageAccountActualName
    location: location
    tags: tags
    skuName: 'Standard_LRS'
    blobServices: {
      containers: [{name: deploymentStorageContainerName}, {name: 'notes'}]
    }
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

module blobRoleAssignmentApi 'app/rbac/storage-access.bicep' = {
  name: 'blobRoleAssignmentapi'
  scope: rg
  params: {
    storageAccountName: storage.outputs.name
    roleDefinitionID: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner
    principalID: apiUserAssignedIdentity.outputs.principalId
  }
}

module functionAppMonitoring 'app/monitoring.bicep' = {
  name: 'functionAppMonitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
  }
}

module appInsightsRoleAssignmentApi 'app/rbac/appinsights-access.bicep' = {
  name: 'appInsightsRoleAssignmentapi'
  scope: rg
  params: {
    appInsightsName: functionAppMonitoring.outputs.applicationInsightsName
    roleDefinitionID: '3913510d-42f4-4e42-8a64-420c390055eb' // Monitoring Metrics Publisher
    principalID: apiUserAssignedIdentity.outputs.principalId
  }
}

module api 'app/api.bicep' = {
  scope: rg
  params: {
    name: functionAppName
    location: location
    tags: tags
    resourceToken: resourceToken
    applicationInsightsName: functionAppMonitoring.outputs.applicationInsightsName
    storageAccountName: storage.outputs.name
    deploymentStorageContainerName: deploymentStorageContainerName
    identityId: apiUserAssignedIdentity.outputs.resourceId
    identityClientId: apiUserAssignedIdentity.outputs.clientId
    appSettings: {
      AZURE_CLIENT_ID: apiUserAssignedIdentity.outputs.clientId
    }
  }
  dependsOn: [
    appInsightsRoleAssignmentApi
    blobRoleAssignmentApi
  ]
}

// Outputs
output AZURE_FUNCTION_NAME string = api.outputs.SERVICE_API_NAME
output AZUREWEBJOBSSTORAGE string = storage.outputs.primaryBlobEndpoint
output AZURE_MANAGED_IDENTITY_CLIENT_ID string = apiUserAssignedIdentity.outputs.clientId
