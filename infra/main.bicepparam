// ============================================================
// PurrfectTodo — Bicep parameter file
// Fill in sqlAdminPassword via --parameters or a secure prompt
// before deploying. Never commit a real password here.
// ============================================================
using './main.bicep'

// ── Required ──────────────────────────────────────────────────
param appName = 'purrfect'

// ── Optional overrides ────────────────────────────────────────
param location = 'westeurope'
param environment = 'production'
param owner = 'PurrfectTodo-team'
param sqlAdminLogin = 'purrfectadmin'

// ── NEVER commit a real password ──────────────────────────────
// Supply at deploy time with:
//   az deployment group create \
//     --parameters infra/main.bicepparam \
//     --parameters sqlAdminPassword="<your-password>"
// Or use a Key Vault reference for fully automated pipelines.
param sqlAdminPassword = ''
