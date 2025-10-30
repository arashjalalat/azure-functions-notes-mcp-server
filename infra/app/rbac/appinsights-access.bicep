param principalID string
param roleDefinitionID string
param appInsightsName string

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource appInsightsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(applicationInsights.id, principalID, roleDefinitionID)
  scope: applicationInsights
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionID)
    principalId: principalID
    principalType: 'ServicePrincipal'
  }
}

output ROLE_ASSIGNMENT_NAME string = appInsightsRoleAssignment.name
