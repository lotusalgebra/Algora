# Algora Production Deployment Checklist

## Pre-Deployment Verification

### Build Status
- [x] Solution builds successfully in Release mode (0 errors, 18 warnings)
- [x] All 109 unit tests pass
- [x] No hardcoded secrets in source code
- [x] `appsettings.Development.json` is gitignored (secrets not in repo)

### Code Quality
- [x] Reports UI converted to Tailwind CSS (6 pages)
- [x] All pages responsive with dark mode support
- [x] Empty state handlers for all tables/charts

---

## Environment Configuration

### Required Environment Variables / Secrets

Replace the `#{PLACEHOLDER}#` tokens in `appsettings.Production.json`:

| Setting | Description | Example |
|---------|-------------|---------|
| `#{DB_PASSWORD}#` | SQL Server password | `StrongP@ssw0rd!` |
| `#{SHOPIFY_API_KEY}#` | Shopify app API key | `abc123...` |
| `#{SHOPIFY_API_SECRET}#` | Shopify app API secret | `shpss_...` |
| `#{SHOPIFY_SHOP_DOMAIN}#` | Default shop domain | `mystore.myshopify.com` |

### Additional Secrets to Configure (via Azure Key Vault / Environment Variables)

```json
{
  "AI": {
    "OpenAi": { "ApiKey": "sk-..." },
    "Anthropic": { "ApiKey": "sk-ant-..." }
  },
  "WhatsApp": {
    "AccessToken": "EAAx...",
    "AppSecret": "...",
    "WebhookVerifyToken": "..."
  },
  "ScraperApi": { "ApiKey": "..." }
}
```

---

## Database Migrations

Run the following SQL scripts in order on the production database:

### Core Tables
1. `AddPlanTables.sql` - Pricing plans
2. `AddBundleTables.sql` - Product bundles
3. `AddReviewTables.sql` - Review system
4. `AddReturnsTables.sql` - Returns management
5. `AddUpsellTables.sql` - Upsell offers
6. `AddAnalyticsTables.sql` - Analytics snapshots

### Feature Tables
7. `CreateMarketingAutomationTables.sql` - Email automations
8. `CreateCustomerHubTables.sql` - Unified inbox, exchanges, loyalty
9. `CreateOperationsManagerTables.sql` - Suppliers, POs, locations
10. `CreateAiAssistantTables.sql` - AI chatbot, SEO, pricing
11. `CreateLabelTemplateTables.sql` - Label designer
12. `CreatePlanFeatureTables.sql` - Feature flags per plan
13. `ChatbotBridgeSetup.sql` - Chatbot escalation

### Data Migrations
14. `UpdatePricingPlans.sql` - Plan prices (Free, Basic $99, Premium $199, Enterprise $299)
15. `UpdatePlanPrices.sql` - Updated pricing

### Column Additions (run if upgrading from older version)
16. `AddMissingOrderColumns.sql`
17. `AddMissingOrderLineColumns.sql`
18. `AddMissingRefundColumns.sql`
19. `AddMissingInventoryPredictionColumns.sql`
20. `AddMissingNextStepAtColumn.sql`
21. `AddMissingCommunicationColumns.sql`
22. `AddMissingEmailColumns.sql`
23. `AddMissingEmailMarketingColumns.sql`
24. `AddEmailRecipientMissingColumns.sql`
25. `AddCommerceInventoryFeatures.sql`
26. `FixOperationsManagerTables.sql`

---

## Deployment Steps

### 1. Pre-Deployment
```bash
# Backup production database
sqlcmd -S prod-server -d Algora_Prod -Q "BACKUP DATABASE Algora_Prod TO DISK='backup.bak'"

# Tag release
git tag -a v1.0.0 -m "Production release v1.0.0"
git push origin v1.0.0
```

### 2. Build & Publish
```bash
# Build for production
dotnet publish Algora.Web/Algora.Web.csproj -c Release -o ./publish

# Or create Docker image
docker build -t algora:v1.0.0 .
```

### 3. Database Migrations
```bash
# Connect to production SQL Server and run migration scripts
sqlcmd -S prod-server -d Algora_Prod -i Scripts/AddPlanTables.sql
# ... (run all scripts in order)
```

### 4. Deploy Application
```bash
# Azure App Service
az webapp deploy --resource-group algora-rg --name algora-web --src-path ./publish

# Or Docker
docker push algora:v1.0.0
kubectl set image deployment/algora algora=algora:v1.0.0
```

### 5. Post-Deployment Verification
- [ ] Health check endpoint responds: `GET /health`
- [ ] Login page loads correctly
- [ ] Dashboard displays data
- [ ] Reports pages render with Tailwind styling
- [ ] Background services start (check logs for "started" messages)

---

## Background Services

The following services run automatically on startup:

| Service | Interval | Description |
|---------|----------|-------------|
| `InventoryPredictionBackgroundService` | Daily | Predict stock levels |
| `ReviewImportBackgroundService` | 5 min | Process review imports |
| `ReviewEmailBackgroundService` | 1 min | Send review request emails |
| `ProductAffinityBackgroundService` | Daily | Calculate product affinities |
| `AnalyticsBackgroundService` | Daily | Generate analytics snapshots |
| `LoyaltyBackgroundService` | Hourly | Process loyalty points/tiers |
| `MarketingAutomationBackgroundService` | 5 min | Process email automations |
| `PurchaseOrderBackgroundService` | Hourly | Auto-generate POs |

---

## Rollback Plan

### Application Rollback
```bash
# Azure App Service
az webapp deployment slot swap -g algora-rg -n algora-web --slot staging --target-slot production

# Or restore previous Docker image
kubectl set image deployment/algora algora=algora:v0.9.0
```

### Database Rollback
```bash
# Restore from backup
sqlcmd -S prod-server -Q "RESTORE DATABASE Algora_Prod FROM DISK='backup.bak' WITH REPLACE"
```

---

## Monitoring & Alerts

### Recommended Monitoring
- Application Insights / Sentry for error tracking
- SQL Server metrics (DTU, query performance)
- Background service health (log "started" messages)
- Response time monitoring

### Key Metrics to Track
- Average response time < 500ms
- Error rate < 1%
- Background service completion rate
- Database connection pool usage

---

## Security Checklist

- [x] No secrets in source control
- [x] Production config uses placeholder tokens
- [ ] HTTPS enforced
- [ ] CORS configured for production domains only
- [ ] Rate limiting enabled
- [ ] SQL injection protection (EF Core parameterized queries)
- [ ] XSS protection (Razor auto-encoding)
- [ ] CSRF tokens on forms

---

## Support Contacts

- **Development Team**: dev@lotusalgebra.com
- **Infrastructure**: infra@lotusalgebra.com
- **On-Call**: PagerDuty rotation

---

*Generated: January 2026*
*Version: 1.0.0*
