# AcxiomCRM — Enterprise Customer Relationship Management System

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC%20%2B%20WebAPI-512BD4.svg)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4.svg)](https://docs.microsoft.com/ef/core/)
[![Tests](https://img.shields.io/badge/Tests-45%20Passed%20(100%25)-brightgreen.svg)]()
[![Security](https://img.shields.io/badge/Security-Identity%20%2B%20Resource--Level%20Auth-success.svg)]()

> **AcxiomCRM** is an interview-ready, full-stack, enterprise-grade Customer Relationship Management (CRM) web application built using **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core**, **ASP.NET Core Identity**, **Bootstrap 5**, **Chart.js**, and **xUnit**. It enforces strict role-based and resource-level authorization across three corporate tiers: **Administrator**, **Sales Manager**, and **Sales Executive**.

---

## 📑 Table of Contents
1. [System Architecture](#system-architecture)
2. [Key Capabilities & Modules](#key-capabilities--modules)
3. [Role & Permission Scoping](#role--permission-scoping)
4. [Prerequisites & Environment](#prerequisites--environment)
5. [Quick Start & Setup](#quick-start--setup)
6. [Seed Demonstration Accounts](#seed-demonstration-accounts)
7. [Automated Testing Suite](#automated-testing-suite)
8. [REST API Documentation](#rest-api-documentation)
9. [Detailed Documentation Suite](#detailed-documentation-suite)
10. [Known Limitations & Production Roadmap](#known-limitations--production-roadmap)

---

## 🏛 System Architecture

The solution follows a clean, modular N-Tier architecture enforcing Separation of Concerns (SoC) and Dependency Inversion:

```
AcxiomCRM/
│
├── AcxiomCRM.Core/             # Pure domain entities, enums, exceptions & constants (zero external dependencies)
│   ├── Entities/               # ApplicationUser, Customer, Lead, Opportunity, FollowUp, Activity, AuditLog
│   ├── Enums/                  # LeadStatus, OpportunityStage, FollowUpType, FollowUpStatus, ActivityType
│   ├── Exceptions/             # BusinessRuleValidationException
│   └── Constants/              # AppRoles (Admin, Manager, SalesExecutive)
│
├── AcxiomCRM.Infrastructure/   # Persistence, EF Core DbContext, Identity config & business services
│   ├── Data/                   # ApplicationDbContext, DatabaseSeeder, Migrations
│   ├── Services/               # Customer, Lead, Opportunity, FollowUp, Activity, Dashboard, Report, User, AuditLog
│   └── Security/               # CurrentUserService (IHttpContextAccessor extraction)
│
├── AcxiomCRM.Web/              # ASP.NET Core MVC presentation layer, Razor Views, REST Web APIs & ViewModels
│   ├── Controllers/            # Account, Dashboard, Customers, Leads, Opportunities, FollowUps, Activities, Users, Reports, AuditLogs
│   ├── ApiControllers/         # CustomersApiController, LeadsApiController, OpportunitiesApiController, AuthApiController
│   ├── ViewModels/             # Strongly typed ViewModels for every view
│   ├── DTOs/                   # Request/Response Data Transfer Objects for REST APIs
│   ├── Views/                  # Polished Razor views (Bootstrap 5, Chart.js, Feather icons)
│   └── wwwroot/                # Static assets, site.css, site.js
│
└── AcxiomCRM.Tests/            # Automated test suite (xUnit, Moq, SQLite in-memory provider)
    ├── Unit/                   # Opportunity, FollowUp, LeadConversion & Customer business rule tests
    ├── Integration/            # REST API controller & authentication security tests
    └── Helpers/                # TestDbContextFactory
```

---

## 🚀 Key Capabilities & Modules

1. **Authentication & Identity Security**:
   - Built on ASP.NET Core Identity with PBKDF2 password hashing.
   - Configurable failed login lockout (5 attempts, 15-minute lockout).
   - Anti-Forgery token validation (`[ValidateAntiForgeryToken]`) on all state-altering forms.
   - Secure HTTP-only authentication cookies and centralized audit logging for auth events.
   - 1-click credential auto-fill buttons on the login page for rapid presentation/evaluation.

2. **Executive & Sales Dashboard**:
   - 8 dynamic KPI cards: Total Customers, Total Leads, Open Leads, Total Opportunities, Open Opportunities, Won Opportunities, Lost Opportunities, and Total Pipeline Value.
   - Interactive Chart.js visualizations:
     - Lead Status Distribution (Doughnut).
     - Opportunity Pipeline Stages (Bar).
     - Monthly Sales & Conversion Trends (Line).
   - Time range filters: *Today*, *This Week*, *This Month*, *Custom Range*.
   - Live Urgent Action Center highlighting overdue and upcoming follow-ups.

3. **Customer Management**:
   - Automated sequential customer codes (`CUST-00001`, `CUST-00002`...).
   - Multi-field search (Name, Email, Phone, Company) and status filters.
   - Uniqueness enforcement on Email and Phone numbers at both database constraint and application service layers.
   - Full relationship inspection showing linked Opportunities, Leads, Follow-ups, and Activities.

4. **Lead Lifecycle & Transactional Conversion**:
   - Supported stages: `New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost`.
   - Priority indicators, expected revenue valuation, source tracking, and owner assignment.
   - **Atomic Transactional Lead Conversion**: When a lead reaches `Qualified`, convert it to a Customer and an optional Opportunity inside a single EF Core database transaction. Duplicate conversions and conversions from `Lost`/`Unqualified` are strictly prohibited.

5. **Sales Pipeline & Opportunity Forecasting**:
   - Pipeline stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
   - Business rule validations: Amount > $0, Probability between 0% and 100%, Expected Close Date cannot be in the past.
   - **Weighted Pipeline Value Calculation**: `Weighted Value = Amount × (Probability / 100)`.
   - Real-time client-side interactive forecast preview on create/edit screens.

6. **Follow-Up Scheduling & Tracking**:
   - Supported channels: `Call`, `Meeting`, `Email`, `Task`.
   - Status transitions: `Planned`, `Completed`, `Missed`, `Cancelled`.
   - Strict scheduling rule: Planned follow-ups cannot be scheduled in the past.
   - Reschedule modal with audit logging.

7. **Omnichannel Activity Logging**:
   - Log customer and lead interactions across Phone Calls, Meetings, Emails, and Tasks with precise timestamps, participants, and outcomes.

8. **User & Role Administration (Admin Only)**:
   - Create CRM staff, assign roles (`Admin`, `Manager`, `Sales Executive`), activate/deactivate accounts, and perform administrative password resets.
   - Defensive safeguard: System prevents deactivating or removing the last active Administrator.

9. **Append-Only Security Audit Logging**:
   - Centralized audit trail recording User ID, Action, Entity Name, Record ID, Changes (Diff), IP Address, and Timestamp.
   - Viewable and searchable exclusively by Administrators.

10. **Enterprise Reports & Formula-Injection-Safe CSV Export**:
    - 8 operational reports: Customer Report, Lead Report, Follow-Up Report, Opportunity Report, Sales Pipeline Report, Lead Conversion Report, User Activity Report, and Audit Report.
    - Tabbed interface with date and owner filters.
    - Single-click CSV export with sanitization against spreadsheet formula injection (protecting against `=SUM(...)`, `+`, `-`, or `@` payloads).

11. **REST Web API**:
    - Authenticated JSON endpoints for `/api/customers`, `/api/leads`, `/api/opportunities`, and `/api/auth`.
    - Proper HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`).
    - Standardized error response envelope (`{ success: false, message: "...", errors: [...] }`).

---

## 🛡 Role & Permission Scoping

Permissions are strictly enforced on the server within business services using `ICurrentUserService`, rather than merely hiding UI links:

| Module / Operation | Administrator | Sales Manager | Sales Executive |
| :--- | :---: | :---: | :---: |
| **All Customers & Pipeline Data** | Full Access (Global) | Full Access (Team Scope) | Assigned Only (Ownership Scope) |
| **Lead Conversion** | Yes | Yes | Yes (Assigned Leads) |
| **Modify Record Ownership** | Yes | Yes | No |
| **User & Role Administration** | Yes | No (`403 Forbidden`) | No (`403 Forbidden`) |
| **Security Audit Logs** | Yes | No (`403 Forbidden`) | No (`403 Forbidden`) |
| **Executive Reports** | All 8 Reports | All 8 Reports | Operational / Assigned Only |
| **Export to CSV** | Yes | Yes | Yes (Scoped records) |

---

## 💻 Prerequisites & Environment

- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.400 or higher)
- **Database Engine**:
  - *Default (Development)*: **SQLite** (`AcxiomCRM.db` in `AcxiomCRM.Web`) — zero setup required.
  - *Production*: **Microsoft SQL Server / Azure SQL** — toggle via `appsettings.json` (`"DatabaseProvider": "SqlServer"`).
- **Browser**: Modern evergreen browser (Chrome, Edge, Firefox, Safari).

---

## ⚡ Quick Start & Setup

### 1. Clone & Build
```powershell
git clone <repo-url>
cd AcxiomCRM
dotnet restore
dotnet build --configuration Release
```

### 2. Database Migration & Initialization
The application automatically creates the database and seeds initial demonstration records on startup. If you wish to apply migrations manually:
```powershell
cd AcxiomCRM.Web
dotnet ef database update --project ../AcxiomCRM.Infrastructure/AcxiomCRM.Infrastructure.csproj
```

### 3. Run the Web Application
```powershell
dotnet run --project AcxiomCRM.Web/AcxiomCRM.Web.csproj --launch-profile http
```
The application will launch on `http://localhost:5053` (or the configured port).

---

## 🔑 Seed Demonstration Accounts

The system automatically initializes role-representative user accounts with pre-populated leads, customers, opportunities, and activities:

| Role | Username / Email | Password | Description & Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.local` | `Admin@12345!` | Global system control, user management, audit logs, all records. |
| **Manager** | `manager@acxiomcrm.local` | `Manager@12345!` | Team pipeline overview, management reports, team record oversight. |
| **Sales Executive** | `sales1@acxiomcrm.local`<br>*(or `sales@acxiomcrm.local`)* | `Sales@12345!` | Individual sales dashboard, assigned customers, leads, and follow-ups. |

*Tip: The login page at `http://localhost:5053/Account/Login` includes **1-click pre-fill buttons** for each of the accounts above.*

---

## 🧪 Automated Testing Suite

The project includes 45 automated tests written in **xUnit**, utilizing **Moq** and **SQLite In-Memory** to test business logic and API endpoints:

```powershell
dotnet test AcxiomCRM.sln --logger "console;verbosity=normal"
```

### Test Coverage Highlights:
- **`OpportunityBusinessRuleTests`**: Verifies rejection of amount $\le 0$, probability outside $[0, 100]$, past close dates, non-existent customer/lead foreign keys, and weighted pipeline valuation.
- **`FollowUpBusinessRuleTests`**: Validates follow-up date policy (cannot schedule planned follow-ups in the past) and referenced entity existence.
- **`LeadConversionBusinessRuleTests`**: Proves atomic conversion from Lead to Customer and Opportunity, prevents duplicate conversions, and enforces status prerequisites.
- **`CustomerValidationTests`**: Enforces email uniqueness, phone uniqueness, and duplicate detection.
- **`CsvSanitizationTests`**: Validates spreadsheet formula injection prevention for characters `=`, `+`, `-`, `@`.
- **`DashboardCalculationTests`**: Validates KPI accuracy, excluding Won/Lost stages from open pipeline calculations.
- **`ApiControllerTests`**: Validates HTTP status codes (`200`, `201`, `400`, `404`) and DTO mapping.
- **`AuthenticationSecurityTests`**: Validates login, lockout, and password complexity.


## 📡 REST API Documentation

The REST API exposes authenticated endpoints under the `/api/` prefix with uniform JSON error and success payloads:

- `POST /api/auth/login`: Authenticate and obtain session cookie.
- `POST /api/auth/logout`: Revoke session.
- `GET  /api/customers`: Paginated, searchable customer list.
- `GET  /api/customers/{id}`: Detailed customer profile.
- `POST /api/customers`: Create customer record (enforces unique email/phone).
- `PUT  /api/customers/{id}`: Update customer profile.
- `DELETE /api/customers/{id}`: Deactivate customer.
- `GET  /api/leads`: Scoped lead list with status filtering.
- `POST /api/leads`: Create new lead.
- `GET  /api/opportunities`: Scoped sales opportunities.
- `POST /api/opportunities`: Create opportunity with business rule validation.





## 📚 Detailed Documentation Suite

Comprehensive architecture, security, database, and interview preparation guides are located in the [`docs/`](docs/) directory:

- 🏛 [Architecture & Layering Guide](docs/ARCHITECTURE.md)
- 🗄 [Relational Database Design & ERD](docs/DATABASE_DESIGN.md)
- 🔒 [Security & Authorization Specifications](docs/SECURITY.md)
- 📡 [REST API Documentation & Schemas](docs/API.md)
- 🧪 [Testing Strategy & Test Catalog](docs/TESTING.md)
- 🎤 [Technical Interview Preparation & Defense](docs/INTERVIEW_PREPARATION.md)
- 📝 [Chronological Development Log](docs/DEVELOPMENT_LOG.md)



## 🔮 Known Limitations & Production Roadmap

1. **Email Service Integration**: The current implementation uses an internal email abstraction (`IEmailSender`). In production, configure SendGrid, AWS SES, or an SMTP provider.
2. **JWT Token Authentication for Mobile**: The current API utilizes ASP.NET Core Identity cookie/session authentication. For third-party mobile or external B2B integrations, integrate OpenIddict or ASP.NET Core JWT Bearer authentication.
3. **Database Scale**: SQLite is active by default for immediate, zero-friction developer setup. Set `UseSqlite: false` and supply a SQL Server connection string in `appsettings.json` for enterprise clustering.
4. **Real-time Notifications**: Follow-up reminders are currently polled via dashboard queries. Integrating ASP.NET Core SignalR will enable real-time browser push notifications.

