# PurrfectTodo — Azure Infrastructure

This folder contains the Bicep IaC templates for the PurrfectTodo application.

## Resources provisioned

| Resource | Name pattern | Notes |
|---|---|---|
| App Service Plan | `asp-purrfect-production` | Linux B1 |
| App Service | `app-purrfect-production` | .NET 10, HTTPS-only |
| App Service staging slot | `app-purrfect-production/staging` | Blue/green swap |
| Azure SQL Server | `sql-purrfect-production-<hash>` | TLS 1.2, Azure services firewall |
| Azure SQL Database | `sqldb-purrfect` | Basic tier, 2 GB |
| Key Vault | `kv-purrfect-<hash>` | RBAC mode, soft-delete 7 days |
| Log Analytics Workspace | `log-purrfect-production` | PerGB2018, 30-day retention |
| Application Insights | `appi-purrfect-production` | Workspace-based |

All resources are tagged: `project=PurrfectTodo`, `environment=production`, `managedBy=bicep`.

## Security model

- The App Service uses a **system-assigned Managed Identity**.
- The Managed Identity is granted the **Key Vault Secrets User** RBAC role on Key Vault.
- The SQL connection string is stored in Key Vault as `SqlConnectionString`.
- App Service reads it via a **Key Vault reference** (`@Microsoft.KeyVault(...)`).
- **No secrets are stored in workflow files, app settings, or source code.**

## First-time deployment (manual)

### Prerequisites
- Azure CLI ≥ 2.60 with Bicep extension: `az bicep install`
- A resource group:
  ```bash
  az group create --name rg-purrfect-production --location westeurope
  ```

### Deploy
```bash
az deployment group create \
  --resource-group rg-purrfect-production \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters sqlAdminPassword="<YourStrongPassword123!>"
```

The SQL admin password is written to Key Vault automatically. You will not need it again for day-to-day operations.

### After first deployment — configure GitHub Actions secrets

Set these four repository secrets (Settings → Secrets → Actions):

| Secret | Value |
|---|---|
| `AZURE_CREDENTIALS` | JSON from `az ad sp create-for-rbac --sdk-auth` (see below) |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID |
| `AZURE_RESOURCE_GROUP` | `rg-purrfect-production` |
| `APP_SERVICE_NAME` | `app-purrfect-production` |

Generate `AZURE_CREDENTIALS`:
```bash
az ad sp create-for-rbac \
  --name "purrfect-github-actions" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-purrfect-production \
  --sdk-auth
```

Copy the full JSON output as the value of `AZURE_CREDENTIALS`.

## Deployment slot swap flow

The CD pipeline (`deploy.yml`) follows this flow:

```
push to main
   ↓
build + test
   ↓
deploy to /staging slot
   ↓
smoke-test staging (HTTP 200/302)
   ↓
swap staging → production  (zero-downtime)
   ↓
verify production health
```

To roll back instantly, re-swap:
```bash
az webapp deployment slot swap \
  --resource-group rg-purrfect-production \
  --name app-purrfect-production \
  --slot production \
  --target-slot staging
```

## Re-deploying infrastructure

Bicep deployments are idempotent. Re-run the `az deployment group create` command
at any time to add or update resources. Existing secrets in Key Vault are updated
only if the connection string changes.
