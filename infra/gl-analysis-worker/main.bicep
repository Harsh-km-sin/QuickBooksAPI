// Provisions the GL Analysis Worker as an Azure Container App:
//   ACR (image storage) + Log Analytics + Container Apps managed environment
//   + a Container App that scales on Service Bus queue length (KEDA), 0..maxReplicas.
// Deployed by .github/workflows/gl-worker-deploy.yml via `az deployment group create`.

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Name of the Container Apps managed environment.')
param environmentName string = 'gl-analysis-env'

@description('Name of the Container App running the worker.')
param containerAppName string = 'gl-analysis-worker'

@description('Name of the Azure Container Registry (must be globally unique, alphanumeric only).')
param acrName string

@description('Full image reference to deploy, e.g. myregistry.azurecr.io/gl-analysis-worker:<tag>.')
param containerImage string

@description('Service Bus queue name the worker consumes.')
param glAnalysisQueueName string = 'gl-analysis'

@secure()
@description('Connection string for the Service Bus namespace hosting the GL analysis queue.')
param serviceBusConnectionString string

@secure()
@description('Connection string for the Blob Storage account holding uploaded GL files.')
param blobStorageConnectionString string

@secure()
@description('ODBC connection string for the Azure SQL database (GL_Runs/GL_Transactions/etc.).')
param sqlConnectionString string

param smtpHost string = ''
param smtpPort string = '587'
param smtpUser string = ''

@secure()
param smtpPassword string = ''

param appBaseUrl string = ''
param logLevel string = 'INFO'
param maxWaitSeconds string = '10'

@description('Minimum replicas. 0 lets the app scale to zero when the queue is empty.')
param minReplicas int = 0

@description('Maximum replicas under load.')
param maxReplicas int = 3

var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d' // built-in "AcrPull" role
)

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${containerAppName}-identity'
  location: location
}

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, identity.id, 'AcrPull')
  scope: acr
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: acrPullRoleDefinitionId
    principalType: 'ServicePrincipal'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${environmentName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: acr.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        { name: 'service-bus-connection', value: serviceBusConnectionString }
        { name: 'blob-storage-connection', value: blobStorageConnectionString }
        { name: 'sql-connection', value: sqlConnectionString }
        { name: 'smtp-password', value: smtpPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'gl-analysis-worker'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'SERVICE_BUS_CONNECTION_STRING', secretRef: 'service-bus-connection' }
            { name: 'GL_ANALYSIS_QUEUE_NAME', value: glAnalysisQueueName }
            { name: 'BLOB_STORAGE_CONNECTION_STRING', secretRef: 'blob-storage-connection' }
            { name: 'SQL_CONNECTION_STRING', secretRef: 'sql-connection' }
            { name: 'SMTP_HOST', value: smtpHost }
            { name: 'SMTP_PORT', value: smtpPort }
            { name: 'SMTP_USER', value: smtpUser }
            { name: 'SMTP_PASSWORD', secretRef: 'smtp-password' }
            { name: 'APP_BASE_URL', value: appBaseUrl }
            { name: 'LOG_LEVEL', value: logLevel }
            { name: 'MAX_WAIT_SECONDS', value: maxWaitSeconds }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'servicebus-queue-length'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: glAnalysisQueueName
                messageCount: '1'
              }
              auth: [
                { secretRef: 'service-bus-connection', triggerParameter: 'connection' }
              ]
            }
          }
        ]
      }
    }
  }
  dependsOn: [
    acrPullRoleAssignment
  ]
}

output acrLoginServer string = acr.properties.loginServer
output containerAppName string = containerApp.name
