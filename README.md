# Ripple Event Ticketing System

A RESTful event ticketing system built with **.NET 10**, **ASP.NET Core Web API**, **Entity Framework Core 10**, and **SQL Server**.

---

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

```text
Ripple.EventTicketingSystem
│
├── Ripple.EventTicketingSystem.API
│   │
│   ├── Controllers
│   │   ├── EventsController.cs
│   │   ├── TicketsController.cs
│   │   └── ReportsController.cs
│   │
│   ├── Middleware
│   │   └── ExceptionHandlingMiddleware.cs
│   │
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
│
├── Ripple.EventTicketingSystem.Application
│   │
│   ├── DTOs
│   │   ├── Events
│   │   ├── Tickets
│   │   └── Reports
│   │
│   ├── Interfaces
│   │   ├── IEventRepository.cs
│   │   ├── IPricingTierRepository.cs
│   │   ├── ITicketRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   └── ITicketingDbContext.cs
│   │
│   ├── Options
│   │   └── TicketingOptions.cs
│   │
│   └── Services
│       ├── EventService.cs
│       ├── TicketService.cs
│       └── ReportService.cs
│
├── Ripple.EventTicketingSystem.Domain
│   │
│   ├── Models
│   │   ├── Event.cs
│   │   ├── PricingTier.cs
│   │   └── Ticket.cs
│   │
│   └── Exceptions
│       └── ...
│
├── Ripple.EventTicketingSystem.Infrastructure
│   │
│   ├── Data
│   │   └── TicketingDbContext.cs
│   │
│   └── Repositories
│       ├── EventRepository.cs
│       ├── PricingTierRepository.cs
│       └── TicketRepository.cs
│
├── Ripple.EventTicketingSystem.Application.Tests
│   │
│   └── Services
│       ├── EventServiceTests.cs
│       ├── TicketServiceTests.cs
│       └── ReportServiceTests.cs
│
└── Ripple.EventTicketingSystem.Infrastructure.Tests
    │
    └── Repositories
        ├── EventRepositoryTests.cs
        ├── PricingTierRepositoryTests.cs
        └── TicketRepositoryTests.cs
```

---

# Architecture

The solution follows a layered architecture with clear separation of responsibilities.

```text
                         ┌──────────────┐                        
                         │     API      │
                         └──────┬───────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
                    ▼                       ▼
             ┌─────────────┐       ┌────────────────┐
             │ Application │◄──────│ Infrastructure │
             └──────┬──────┘       └───────┬────────┘
                    │                      │
                    ▼                      ▼
              ┌──────────┐          ┌──────────────┐
              │  Domain  │          │ SQL Server   │
              └──────────┘          └──────────────┘
```

# Prerequisites

Before running the application, install:

1. **Visual Studio 2022** with the ASP.NET and web development workload
2. **.NET 10 SDK**
3. **SQL Server**, SQL Server Express, or SQL Server LocalDB
4. Optional: **SQL Server Management Studio (SSMS)**

Verify the .NET SDK:

```bash
dotnet --version
```

The solution targets:

```text
net10.0
```

---

# Database Setup

The application uses **SQL Server**.

Update the connection string in:

```text
Ripple.EventTicketingSystem.API/appsettings.json
```

Example using SQL Server Express:

```json
{
  "ConnectionStrings": {
    "RippleEventTracking": "Server=localhost\\SQLEXPRESS;Database=RippleEventTracking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

If using SQL Server LocalDB:

```json
{
  "ConnectionStrings": {
    "RippleEventTracking": "Server=(localdb)\\MSSQLLocalDB;Database=RippleEventTracking;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Adjust the server name and authentication settings according to the local SQL Server installation.

---

# Entity Framework Core Database

If EF Core migrations are included in the solution, update the database with:

```bash
dotnet ef database update
```

If the EF Core CLI tool is not installed:

```bash
dotnet tool install --global dotnet-ef
```

Then:

```bash
dotnet ef database update
```

If migrations are not included with the submitted solution, the database schema can be created using the supplied SQL database script.

---

# Running the Application

## Visual Studio

1. Open:

```text
Ripple.EventTicketingSystem.slnx
```

2. Set:

```text
Ripple.EventTicketingSystem.API
```

as the startup project.

3. Press **F5** or **Ctrl + F5**.

The API will start using the configured HTTPS port.

---

## Command Line

From the API project directory:

```bash
cd Ripple.EventTicketingSystem.API
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

The exact port may differ depending on the local `launchSettings.json`.

Swagger provides an easy way to explore and test the REST API without requiring a separate API client.

---
# Running Tests

From the solution directory:

```bash
dotnet test
```

Or run the tests through Visual Studio:

```text
Test
  → Test Explorer
  → Run All Tests
```

---

# API Functionality

The API provides endpoints for:

### Events

```text
GET    /api/events
GET    /api/events/{id}
POST   /api/events
PUT    /api/events/{id}
DELETE /api/events/{id}
```

### Tickets

```text
POST /api/tickets
GET  /api/tickets/events/{eventId}/availability
```

### Reports

```text
GET /api/reports/sales-summary
```

The exact routes can be explored through Swagger/OpenAPI.