# ReportHub

ReportHub is an ASP.NET Core MVC application for running SQL-based reports with role-based access control, centralized audit logging, and an admin console for managing data sources, users, and reports. It is designed as a lightweight internal reporting panel with clear ownership, traceability, and operational visibility.

## Highlights
- Role-based access (admin, ik, mali, user) with per-report permissions.
- Report runner supports parameterized stored procedures and export to Excel.
- Central audit log captures user actions (login/logout, report runs, user/data source/report changes).
- Admin console for data sources, report catalog, and user management.
- Clean, responsive UI with shared layout and server-side filtering.

## Requirements
- .NET 8 SDK
- SQL Server

## Project structure
- `ReportPanel/` - Main ASP.NET Core MVC app.
- `ReportPanel/Controllers` - Auth, admin, reports, profile, logs, dashboard.
- `ReportPanel/Views` - Razor views for the UI.
- `ReportPanel/Database` - Schema and seed scripts.
- `ReportPanel/Models` - EF Core entities and DbContext.
- `ReportPanel/Services` - Audit logging and password hashing.
- `ReportPanel.Tests/` - Automated tests for core utilities.

## Local setup
1) Configure connection strings in `ReportPanel/appsettings.json`.
2) Create the database and tables using scripts in `ReportPanel/Database`.
3) Run the app:

```
cd ReportPanel
dotnet run
```

## Admin and roles
- `Admin` area allows CRUD for users, reports, and data sources.
- Report visibility is controlled by a comma-separated role list per report.
- Logs are visible only to admin users.

## Audit logging
The audit log is centralized and stored in a single table. It captures:
- Authentication events (login/logout).
- Profile updates and password changes.
- User, report, and data source CRUD.
- Report runs and exports.
- Data source connection tests.

## Tests
```
cd ..
dotnet test reporthub.sln
```

## Configuration notes
- Do not commit real passwords or production connection strings.
- Use placeholders in config files and keep secrets in `appsettings.Development.json` (ignored by git).
- Build outputs are ignored via `.gitignore`.
