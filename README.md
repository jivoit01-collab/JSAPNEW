# JSAPNEW — Enterprise Resource Planning (ERP) API

A comprehensive, multi-module ERP backend built on **ASP.NET Core 10** with SQL Server and SAP HANA integration. The system manages budget workflows, inventory, procurement, payments, and more across multiple business divisions.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 (.NET 10.0) |
| Language | C# |
| Data Access | Dapper + Stored Procedures |
| Primary DB | Microsoft SQL Server / Azure SQL |
| Secondary DB | SAP HANA (multi-division) |
| Authentication | JWT Bearer Tokens + BCrypt |
| Documentation | Swagger / OpenAPI |
| Notifications | Firebase Cloud Messaging |
| File Transfer | SSH.NET |
| SAP Integration | SAP Service Layer API + Sap.Data.Hana.Net |
| Frontend (Views) | Razor Pages + Bootstrap + jQuery |

---

## Architecture

The project follows a layered Service-Oriented Architecture:

```
HTTP Request
    │
    ▼
Controllers (37+)          — Route handling, request/response shaping
    │
    ▼
Service Interfaces (23+)   — Contracts defined per module
    │
    ▼
Service Implementations    — Business logic
    │
    ▼
Dapper + Stored Procs      — SQL Server / SAP HANA data access
```

All services are registered via ASP.NET Core Dependency Injection in `Program.cs`. Most use `AddScoped`; singletons are used for long-lived services (notifications, SSH).

---

## Modules

### Authentication & User Management
- Login, registration, password change
- JWT access tokens (60-min expiry) + refresh tokens (7-day expiry)
- Role-based access control with a custom `CheckUserPermissionAttribute`
- Permission templates tied to user-company relationships

### Budget Management
- Budget creation, sub-budget allocation, category-based budgeting
- Multi-step approval and rejection workflows
- Budget insights and summaries per department/company

### BOM (Bill of Materials)
- Product recipe management with components, materials, and resources
- BOM versioning and updates
- Approval flow with SAP product tree sync

### Inventory / GIGO (Goods In – Goods Out)
- Session-based inventory counting
- Audit trails and warehouse management
- Count entry tracking across sessions

### Payment & Expense Management
- Advance requests, vendor expense tracking
- Invoice payment records and approvals
- Payment status lifecycle management

### Procurement
- Item master and supplier/Business Partner (BP) management
- Quality Control (QC) workflows
- Purchase Requisition & Dispatch Orders (PRDO)

### Document Management
- Document dispatch with approval flows
- Upload support for BOM, BP Master, Tickets

### Task & Ticket Management
- Task creation, delegation, and status tracking
- Support ticket system with file attachments

### Reports & Dashboards
- Role-specific dashboards (IT Standards, Budget, Avatar, etc.)
- Audit trails and report generation

### Notifications
- Firebase push notifications with token caching
- SMTP email notifications
- Background notification queue management

### SAP Integration
- Direct SAP Service Layer API calls
- Multi-division HANA connections (Oil, Beverages, Mart)
- Product tree synchronization

---

## Project Structure

```
JSAPNEW/
├── Controllers/            API + MVC controllers (37+)
├── Services/
│   ├── Interfaces/         Service contracts (23+)
│   └── Implementation/     Business logic (26+)
├── Models/                 DTOs and view models (22 files)
├── Data/
│   └── Entities/           Domain entities (e.g., User.cs)
├── Filters/                Custom action filters (permission checks)
├── Views/                  Razor templates for MVC views
│   ├── Login/
│   ├── DashboardWeb/
│   ├── BPmasterweb/
│   ├── QcWeb/
│   ├── TaskWeb/
│   ├── TicketsWeb/
│   └── ...
├── wwwroot/                Static assets
│   ├── js/
│   ├── css/
│   ├── lib/                Bootstrap, jQuery
│   └── Uploads/            BOM, BPmaster, Ticket files
├── Program.cs              App startup and DI configuration
├── appsettings.json        Main configuration
└── JSAPNEW.csproj          Project file and dependencies
```

---

## API Routes

All REST endpoints are prefixed with `/api/[controller]`.

| Module | Sample Endpoints |
|---|---|
| Auth | `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/changepassword` |
| BOM | `GET /api/bom/getwarehouse`, `POST /api/bom/create`, `POST /api/bom/approve` |
| Budget | GET/POST for budgets, allocations, approvals, summaries |
| GIGO | Session management, count entries, audit records |
| Payments | Advance requests, expense tracking, invoice approvals |
| Reports | Dashboard data per role and division |
| Notifications | Token registration, notification dispatch |

Full API documentation is available via Swagger at `/swagger` when running in development.

---

## Database Connections

The application connects to multiple databases configured in `appsettings.json`:

| Key | Purpose |
|---|---|
| `DefaultConnection` | Primary MSSQL database |
| `AzureConnection` | Azure SQL variant |
| `LiveHanaConnection` | SAP HANA — Oil division |
| `LiveBevHanaConnection` | SAP HANA — Beverages division |
| `LiveMartHanaConnection` | SAP HANA — Mart division |

The active environment (Test / Live) is controlled by the `ActiveEnvironment` setting.

---

## Configuration Highlights

| Setting | Default |
|---|---|
| JWT Expiry | 60 minutes |
| Refresh Token Expiry | 7 days |
| Session Timeout | 7 days |
| DB Command Timeout | 180 seconds |
| CORS Origin | `http://localhost:3000` |

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server instance (or Azure SQL)
- SAP HANA drivers (if using SAP integration)
- Firebase service account JSON (for push notifications)

### Run locally

```bash
dotnet restore
dotnet run
```

The app defaults to the `Login` controller's `Index` view on startup. Swagger UI is available at `/swagger` in development mode.

### Build for production

```bash
dotnet publish -c Release
```

---

## Key Design Decisions

- **Dapper over EF Core** — lightweight, full control over SQL; stored procedures handle most business logic in the database layer.
- **Multi-tenancy via company parameter** — all queries are scoped to a company, enabling multi-division support without separate databases (for MSSQL).
- **Custom encryption service** — password encryption via `Encryption.cs` in addition to BCrypt hashing.
- **Permission filter** — `CheckUserPermissionAttribute` enforces role/template-based access at the controller action level.
- **Singleton notification service** — Firebase token caching and background queue prevent duplicate pushes across requests.
