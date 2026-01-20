# Algora - Application Features & Module Documentation

## Overview
Algora is a comprehensive e-commerce management platform built with ASP.NET Core Razor Pages. It provides tools for order management, inventory prediction, marketing automation, AI-powered content generation, and customer experience management.

---

## Pricing Plans

| Plan | Monthly Price | Order Limit | Product Limit | Features |
|------|---------------|-------------|---------------|----------|
| **Free** | $0 | 100 | 50 | Basic features |
| **Basic** | $99 | 500 | 250 | WhatsApp, Email Campaigns |
| **Premium** | $199 | 2,000 | 1,000 | +SMS, Advanced Reports |
| **Enterprise** | $299 | Unlimited | Unlimited | +API Access, Customer Portal, Full features |

---

## Module Directory

### MAIN
| Feature | URL | Description |
|---------|-----|-------------|
| Dashboard | `/dashboard` | Main dashboard with sales statistics, revenue charts, active users, and sales by country |

---

### COMMERCE

#### Orders
| Feature | URL | Description |
|---------|-----|-------------|
| Orders List | `/orders` | View all orders with filtering, sorting, and CSV export |
| Order Details | `/orders/{id}` | Complete order details, customer info, addresses, items |
| Create Order | `/orders/create` | Create manual orders with customer info and line items |
| Edit Order | `/orders/edit/{id}` | Edit existing order details and shipping information |
| Delete/Close Order | `/orders/delete/{id}` | Close or cancel an order |
| Edit Status | `/orders/edit-status/{id}` | Update order fulfillment status with tracking info |
| Invoice | `/orders/invoice/{id}` | View printable invoice for an order |
| Invoice Download | `/orders/invoice/download/{id}` | Download PDF invoice |

#### Products
| Feature | URL | Description |
|---------|-----|-------------|
| Products List | `/products` | View all products with details, pricing, SKU, stock status |
| Product Details | `/products/{id}` | View product details |
| Create Product | `/products/create` | Create new product |
| Edit Product | `/products/edit/{id}` | Edit existing product |
| Delete Product | `/products/delete/{id}` | Delete product |

#### Bundles
| Feature | URL | Description |
|---------|-----|-------------|
| Bundles (Public) | `/bundles` | Public bundles display showing fixed and mix-and-match offerings |
| Bundle Details | `/bundles/{id}` | View bundle details |
| Bundle Builder | `/bundles/builder` | Interactive bundle builder interface |
| Admin - Bundle List | `/bundles/admin` | Admin bundle management dashboard |
| Admin - Create Bundle | `/bundles/admin/create` | Create new bundle |
| Admin - Edit Bundle | `/bundles/admin/edit/{id}` | Edit bundle |
| Admin - Analytics | `/bundles/admin/analytics` | Bundle performance analytics |
| Admin - Settings | `/bundles/admin/settings` | Bundle settings configuration |

---

### MARKETING

#### Upsell
| Feature | URL | Description |
|---------|-----|-------------|
| Upsell Dashboard | `/upsell` | Post-purchase upsell dashboard with active offers and conversions |
| Upsell Offers | `/upsell/offers` | Manage upsell offers |
| Create Offer | `/upsell/offers/create` | Create new upsell offer |
| Edit Offer | `/upsell/offers/edit/{id}` | Edit upsell offer |
| Experiments | `/upsell/experiments` | A/B testing experiments list |
| Experiment Details | `/upsell/experiments/{id}` | View experiment details and results |
| Create Experiment | `/upsell/experiments/create` | Create new A/B experiment |
| Product Affinities | `/upsell/affinities` | Product affinity management |
| Upsell Settings | `/upsell/settings` | Upsell configuration settings |

#### Reviews
| Feature | URL | Description |
|---------|-----|-------------|
| Reviews Dashboard | `/reviews/admin` | Reviews management dashboard |
| Reviews List | `/reviews/admin/list` | View all product reviews |
| Import Reviews | `/reviews/admin/import` | Import reviews from external sources |
| Reviews Settings | `/reviews/admin/settings` | Review collection and display settings |

#### Analytics
| Feature | URL | Description |
|---------|-----|-------------|
| Analytics Dashboard | `/analytics` | Revenue trends, cost breakdown, top products |
| Product Analytics | `/analytics/products` | Product performance analytics |
| Customer Lifetime Value | `/analytics/clv` | CLV analysis and customer segmentation |
| Ads Spend | `/analytics/adsspend` | Ad spend tracking and ROAS metrics |

#### Abandoned Carts
| Feature | URL | Description |
|---------|-----|-------------|
| Abandoned Checkouts | `/abandonedcheckouts` | View and recover abandoned checkouts |

---

### AI TOOLS

| Feature | URL | Description |
|---------|-----|-------------|
| AI Dashboard | `/ai` | AI tools hub with usage stats and quick access |
| Product Descriptions | `/ai/descriptions` | AI-powered product description generator |
| SEO Optimizer | `/ai/seo` | AI SEO meta tag generator with scoring |
| Alt Text Generator | `/ai/alttext` | Bulk alt text generation for product images |
| Pricing Optimizer | `/ai/pricing` | AI-powered pricing suggestions with margin analysis |
| Chatbot Config | `/ai/chatbot` | Customer support chatbot configuration and analytics |
| Bulk Generate | `/ai/bulkgenerate` | Bulk AI content generation for multiple products |

---

### OPERATIONS

| Feature | URL | Description |
|---------|-----|-------------|
| Operations Dashboard | `/operations` | Overview with suppliers, pending orders, locations, alerts |

#### Suppliers
| Feature | URL | Description |
|---------|-----|-------------|
| Suppliers List | `/operations/suppliers` | View and manage suppliers |
| Supplier Details | `/operations/suppliers/{id}` | View supplier details and products |
| Create Supplier | `/operations/suppliers/create` | Add new supplier |
| Edit Supplier | `/operations/suppliers/edit/{id}` | Edit supplier information |

#### Purchase Orders
| Feature | URL | Description |
|---------|-----|-------------|
| Purchase Orders List | `/operations/purchaseorders` | View all purchase orders |
| PO Details | `/operations/purchaseorders/{id}` | View purchase order details |
| Create PO | `/operations/purchaseorders/create` | Create new purchase order |

#### Locations & Inventory
| Feature | URL | Description |
|---------|-----|-------------|
| Locations | `/operations/locations` | Manage warehouse/store locations |
| Barcodes | `/operations/barcodes` | Generate product barcodes |
| Thresholds | `/operations/thresholds` | Set inventory reorder thresholds |

---

### INVENTORY

| Feature | URL | Description |
|---------|-----|-------------|
| Inventory Predictions | `/inventory` | AI-powered inventory predictions and stock forecasting |
| Inventory Alerts | `/inventory/alerts` | Low stock and reorder alerts |
| Inventory Settings | `/inventory/settings` | Alert thresholds and notification preferences |

---

### CUSTOMER HUB

| Feature | URL | Description |
|---------|-----|-------------|
| Customer Hub Dashboard | `/customer-hub` | Overview of conversations, exchanges, loyalty program |

#### Unified Inbox
| Feature | URL | Description |
|---------|-----|-------------|
| Inbox | `/customer-hub/inbox` | Unified inbox for all customer messages (Email, SMS, WhatsApp, Social) |
| Conversation | `/customer-hub/inbox/{id}` | View and reply to conversation thread |

#### Exchanges
| Feature | URL | Description |
|---------|-----|-------------|
| Exchanges List | `/customer-hub/exchanges` | View all exchange requests |
| Exchange Details | `/customer-hub/exchanges/{id}` | Process exchange request |
| Create Exchange | `/customer-hub/exchanges/create` | Create new exchange request |

#### Loyalty Program
| Feature | URL | Description |
|---------|-----|-------------|
| Loyalty Dashboard | `/customer-hub/loyalty` | Loyalty program overview and stats |
| Loyalty Members | `/customer-hub/loyalty/members` | View and manage loyalty members |
| Loyalty Rewards | `/customer-hub/loyalty/rewards` | Manage rewards catalog |
| Loyalty Settings | `/customer-hub/loyalty/settings` | Program configuration, tiers, points |

#### Customer Portal (Public)
| Feature | URL | Description |
|---------|-----|-------------|
| Portal Home | `/customer-hub/portal` | Customer self-service portal |
| Portal Chat | `/customer-hub/portal/chat` | AI-powered customer chat support |
| Portal Orders | `/customer-hub/portal/orders` | Customer order history |
| Portal Loyalty | `/customer-hub/portal/loyalty` | Customer loyalty points and rewards |

---

### RETURNS

| Feature | URL | Description |
|---------|-----|-------------|
| Returns Dashboard | `/returns` | Returns overview with analytics by reason and product |
| Returns List | `/returns/list` | View all return requests |
| Return Details | `/returns/{id}` | Process return request |
| Return Reasons | `/returns/reasons` | Configure return reason codes |
| Returns Settings | `/returns/settings` | Return policy and automation settings |

#### Customer Return Portal (Public)
| Feature | URL | Description |
|---------|-----|-------------|
| Request Return | `/returns/request` | Customer return request form |
| Return Status | `/returns/status/{id}` | Track return status |
| Return Label | `/returns/label/{id}` | Download return shipping label |

---

### SETTINGS

| Feature | URL | Description |
|---------|-----|-------------|
| Account Profile | `/account/profile` | User account settings |
| Plans & Billing | `/plans` | View and manage subscription plans |
| Change Plan | `/plans/change` | Upgrade or downgrade subscription |

---

### AUTHENTICATION

| Feature | URL | Description |
|---------|-----|-------------|
| Login | `/auth/login` | User login page |
| Register | `/auth/register` | New user registration |
| Logout | `/auth/logout` | User logout |
| Access Denied | `/auth/accessdenied` | Access denied error page |

---

### ADMIN

| Feature | URL | Description |
|---------|-----|-------------|
| Plan Requests | `/admin/planrequests` | Manage plan upgrade/downgrade requests |
| Clients | `/admin/clients` | View and manage all clients |
| Plan Features | `/admin/plan-features` | Configure features available per plan |

---

### CUSTOMER PORTAL (Enterprise Only)

> **Note:** The Customer Portal is a standalone branded portal for your customers. It runs as a separate application and is only available on the Enterprise plan.

#### Authentication
| Feature | URL | Description |
|---------|-----|-------------|
| Login | `/Auth/Login` | Customer login with email/password |
| Register | `/Auth/Register` | Customer registration with custom fields |
| Forgot Password | `/Auth/ForgotPassword` | Password reset request |
| Reset Password | `/Auth/ResetPassword` | Password reset with token |

#### Orders
| Feature | URL | Description |
|---------|-----|-------------|
| Order History | `/Orders` | View all customer orders with filtering |
| Order Details | `/Orders/Details/{id}` | View order details, tracking, invoice download |
| Reorder | - | One-click reorder from previous orders |

#### Returns
| Feature | URL | Description |
|---------|-----|-------------|
| Request Return | `/Orders/RequestReturn/{id}` | Submit return/exchange request |
| Return Status | `/Orders/ReturnStatus/{id}` | Track return request status |
| Return History | `/Orders/Returns` | View all return requests |

#### Account
| Feature | URL | Description |
|---------|-----|-------------|
| Profile | `/Account/Profile` | View and edit profile information |
| Change Password | `/Account/ChangePassword` | Update account password |

#### Theme Configuration (Admin)
| Setting | Description |
|---------|-------------|
| Logo & Branding | Custom logo, favicon, store name |
| Colors | Primary, secondary, accent, background colors |
| Typography | Font family for body and headings |
| Button Style | Rounded, pill, or square buttons |
| Custom CSS | Additional CSS overrides |

#### Custom Fields (Admin)
| Page | Default Fields | Custom Fields |
|------|----------------|---------------|
| Registration | Email, Password, First Name, Last Name | Phone, Date of Birth, Address, Custom |
| Profile | First Name, Last Name, Email, Phone | Date of Birth, Address, Custom |

---

### SYSTEM

| Feature | URL | Description |
|---------|-----|-------------|
| Setup Wizard | `/setup` | Initial application setup |
| Privacy Policy | `/privacy` | Privacy policy page |
| Error Page | `/error` | Error handling page |

---

## Background Services

The application includes several background services for automation:

| Service | Description |
|---------|-------------|
| `InventoryPredictionBackgroundService` | Calculates inventory predictions and stockout forecasts |
| `ProductAffinityBackgroundService` | Analyzes product purchase patterns for upsell recommendations |
| `ReviewImportBackgroundService` | Imports reviews from external sources |
| `ReviewEmailBackgroundService` | Sends review request emails to customers |
| `MarketingAutomationBackgroundService` | Processes email automations and win-back campaigns |
| `AnalyticsBackgroundService` | Generates daily analytics snapshots |
| `PurchaseOrderBackgroundService` | Automates purchase order creation based on thresholds |
| `LoyaltyBackgroundService` | Processes loyalty tier evaluations and points expiration |

---

## API Endpoints

The application exposes REST API endpoints at `/api/` for integration with external systems (requires Enterprise plan with API Access).

---

## Technology Stack

- **Framework**: ASP.NET Core 10.0 with Razor Pages
- **Database**: SQL Server with Entity Framework Core
- **UI**: Tailwind CSS (Soft UI Dashboard Pro theme)
- **AI**: OpenAI GPT-4o-mini integration
- **Messaging**: WhatsApp Business API, Twilio SMS
- **Email**: SMTP with template support
- **Authentication**: Cookie-based with Shopify OAuth support

---

*Document generated: December 2024*
