// ============================================================
// PurrfectTodo — Azure Infrastructure
// Deploys: App Service Plan, App Service (+ staging slot),
//          Azure SQL Server + Database, Key Vault,
//          System-assigned Managed Identity, Log Analytics,
//          Application Insights
// ============================================================

@description('Short name used as a prefix for all resources (lowercase, 3-12 chars).')
@minLength(3)
@maxLength(12)
param appName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SQL Server administrator login name.')
param sqlAdminLogin string

@description('SQL Server administrator password — stored in Key Vault immediately, never used afterwards.')
@secure()
param sqlAdminPassword string

@description('Environment tag (production | staging | dev).')
@allowed(['production', 'staging', 'dev'])
param environment string = 'production'

@description('Project owner tag.')
param owner string = 'PurrfectTodo-team'

// ── Derived names (unique per subscription + app name) ────────────────────────
var suffix           = uniqueString(resourceGroup().id, appName)
var appServicePlanName = 'asp-${appName}-${environment}'
var appServiceName   = 'app-${appName}-${environment}'
var sqlServerName    = 'sql-${appName}-${environment}-${take(suffix,6)}'
var sqlDbName        = 'sqldb-${appName}'
var keyVaultName     = 'kv-${appName}-${take(suffix,8)}'
var logAnalyticsName = 'log-${appName}-${environment}'
var appInsightsName  = 'appi-${appName}-${environment}'

// ── Common tags ───────────────────────────────────────────────────────────────
var commonTags = {
  project: 'PurrfectTodo'
  environment: environment
  owner: owner
  managedBy: 'bicep'
}

// ══════════════════════════════════════════════════════════════
// Log Analytics Workspace
// ══════════════════════════════════════════════════════════════
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: commonTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ══════════════════════════════════════════════════════════════
// Application Insights
// ══════════════════════════════════════════════════════════════
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: commonTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ══════════════════════════════════════════════════════════════
// App Service Plan  (Linux B1)
// ══════════════════════════════════════════════════════════════
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  tags: commonTags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true   // required for Linux plans
  }
}

// ══════════════════════════════════════════════════════════════
// App Service  (.NET 10 on Linux)
// ══════════════════════════════════════════════════════════════
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  tags: commonTags
  identity: {
    type: 'SystemAssigned'   // Managed Identity — no stored credentials
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        // Key Vault references — no plaintext secrets in app settings
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=SqlConnectionString)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

// ── Staging deployment slot ────────────────────────────────────────────────────
resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = {
  name: 'staging'
  parent: appService
  location: location
  tags: union(commonTags, { slot: 'staging' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false    // slots on Basic plan: alwaysOn not supported
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=SqlConnectionString)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
      ]
    }
  }
}

// ══════════════════════════════════════════════════════════════
// Azure SQL Server
// ══════════════════════════════════════════════════════════════
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: commonTags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'   // Allow Azure services access (see firewall rule below)
  }
}

// Allow Azure services to reach SQL (e.g. App Service outbound IPs)
resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: 'AllowAzureServices'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── SQL Database (Basic tier) ─────────────────────────────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: sqlDbName
  parent: sqlServer
  location: location
  tags: commonTags
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648   // 2 GB
    zoneRedundant: false
  }
}

// ══════════════════════════════════════════════════════════════
// Key Vault
// ══════════════════════════════════════════════════════════════
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: commonTags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true          // Use RBAC, not legacy access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false           // false allows clean teardown in dev/test
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ── Store SQL connection string as Key Vault secret ───────────────────────────
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource kvSecretSqlConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'SqlConnectionString'
  parent: keyVault
  properties: {
    value: sqlConnectionString
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// ══════════════════════════════════════════════════════════════
// RBAC: Grant App Service Managed Identity → Key Vault Secrets User
// ══════════════════════════════════════════════════════════════
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'  // Key Vault Secrets User

resource kvRoleAppService 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appService.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Also grant staging slot identity access to Key Vault
resource kvRoleStagingSlot 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, stagingSlot.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: stagingSlot.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ══════════════════════════════════════════════════════════════
// Outputs
// ══════════════════════════════════════════════════════════════
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output stagingSlotUrl string = 'https://${stagingSlot.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output logAnalyticsWorkspaceId string = logAnalytics.id
output managedIdentityPrincipalId string = appService.identity.principalId
