# Ripple Event Ticketing System

A RESTful event ticketing system built with **.NET 10**, **ASP.NET Core Web API**, **Entity Framework Core 10**, and **SQL Server**.

The application provides event management, ticket purchasing with inventory control, event availability, and sales reporting.


# Technology Stack

* **.NET 10**
* **ASP.NET Core Web API**
* **C#**
* **Entity Framework Core 10**
* **SQL Server**
* **MSTest**
* **Moq**
* **MockQueryable**
* **OpenAPI / Swagger**

---

# Solution Structure

```
Ripple.EventTicketingSystem
│
├── Ripple.EventTicketingSystem.API
│   ├── Controllers
│   │   ├── EventsController.cs
│   │   ├── TicketsController.cs
│   │   └── ReportsController.cs
│   ├── Middleware
│   └── Program.cs
│
├── Ripple.EventTicketingSystem.Application
│   ├── DTOs
│   ├── Interfaces
│   └── Services
│       ├── EventService.cs
│       ├── TicketService.cs
│       └── ReportService.cs
│
├── Ripple.EventTicketingSystem.Domain
│   ├── Models
│   │   ├── Event.cs
│   │   ├── PricingTier.cs
│   │   └── Ticket.cs
│   └── Exceptions
│
├── Ripple.EventTicketingSystem.DBContext
│   └── Data
│       └── TicketingDbContext.cs
│
└── Tests
    └── Ripple.EventTicketingSystem.Application.Tests
        └── Services
            ├── EventServiceTests.cs
            ├── TicketServiceTests.cs
            └── ReportServiceTests.cs
```

## Architecture

The solution follows a layered architecture:

```text
API
 │
 ▼
Application
 │
 ▼
Domain

DBContext ───────────────┘
```



### DBContext

Contains:

* Entity Framework Core DbContext
* SQL Server configuration
* Entity mappings
* Database-specific configuration

`ITicketingDbContext` is defined in the Application layer and implemented by `TicketingDbContext`. This keeps database implementation details out of the Application layer.

---

# Prerequisites

Before running the application, install:

1. **Visual Studio 2022** with ASP.NET and web development workload
2. **.NET 10 SDK**
3. **SQL Server** or SQL Server Express
4. Optional: SQL Server Management Studio (SSMS)

Verify the .NET SDK:

```bash
dotnet --version
```

The project targets:

```text
net10.0
```

---

# Database Setup

The application uses SQL Server.

Update the connection string in:

```text
Ripple.EventTicketingSystem.API/appsettings.json
```

Example:

```json
{
  "ConnectionStrings": {
    "RippleEventTracking": "Server=localhost\\SQLEXPRESS;Database=RippleEventTracking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Adjust the server name according to your local SQL Server installation.

For example, if using LocalDB:

```json
{
  "ConnectionStrings": {
    "RippleEventTracking": "Server=(localdb)\\MSSQLLocalDB;Database=RippleEventTracking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Create the Database

From the solution directory, run the EF Core migrations if migrations are included:

```bash
dotnet ef database update
```

If the EF Core CLI tool is not installed:

```bash
dotnet tool install --global dotnet-ef
```

If migrations are not included with the submitted solution, the database schema can be created using the supplied SQL database script.

---

# Running the Application

### Visual Studio

1. Open:

```text
Ripple.EventTicketingSystem.slnx
```

2. Set `Ripple.EventTicketingSystem.API` as the startup project.
3. Press **F5** or **Ctrl+F5**.

The application will start using the configured HTTPS port.

### Command Line

From the API project directory:

```bash
dotnet run
```

---

# Swagger / OpenAPI

When running in the Development environment, Swagger UI is available at:

```text
https://localhost:7077/swagger
```

The OpenAPI document is available at:

```text
https://localhost:7077/openapi/v1.json
```

The exact port may differ depending on the local launch settings.

Swagger can be used to test the API without requiring a separate API client.

---


# Testing

The solution contains a separate unit test project:

```text
Ripple.EventTicketingSystem.Application.Tests
```

Tests use:

* MSTest
* Microsoft.NET.Test.Sdk / VSTest
* Moq
* MockQueryable

Run all tests from the solution directory:

```bash
dotnet test
```

Or run tests from Visual Studio using:

**Test → Test Explorer → Run All Tests**


