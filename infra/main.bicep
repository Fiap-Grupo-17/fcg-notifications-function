@description('Azure region. Use the same region as the student subscription when possible.')
param location string = resourceGroup().location

@description('Globally unique Function App name, e.g. func-fcg-notifications-g17')
param functionAppName string

@description('AMQP/AMQPS connection to the shared RabbitMQ (local, CloudAMQP or Azure-hosted).')
@secure()
param rabbitMqConnection string

@description('MassTransit queue for UserCreatedEvent')
param userCreatedQueue string = 'UserCreated'

@description('MassTransit queue for PaymentProcessedEvent')
param paymentProcessedQueue string = 'PaymentProcessed'

var storageAccountName = 'st${uniqueString(resourceGroup().id, functionAppName)}'
var appInsightsName = 'appi-${functionAppName}'
var hostingPlanName = 'plan-${functionAppName}'

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
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
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'RabbitMQConnection'
          value: rabbitMqConnection
        }
        {
          name: 'UserCreatedQueue'
          value: userCreatedQueue
        }
        {
          name: 'PaymentProcessedQueue'
          value: paymentProcessedQueue
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output appInsightsName string = appInsights.name
output healthUrl string = 'https://${functionApp.properties.defaultHostName}/api/health'
