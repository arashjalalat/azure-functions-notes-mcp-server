param principalID string
param roleDefinitionID string
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageAccountName
}

resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, principalID, roleDefinitionID)
  scope: storageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionID)
    principalId: principalID
    principalType: 'ServicePrincipal'
  }
}

output ROLE_ASSIGNMENT_NAME string = storageRoleAssignment.name
