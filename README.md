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
---
# Key Design Decisions

## Explicit Repositories

The solution uses explicit repositories rather than a generic repository.

This makes persistence operations more meaningful to each domain area and avoids hiding useful query behavior behind generic CRUD methods.

---

## Dependency Inversion

Application services depend on interfaces:

```text
Application
    │
    ├── IEventRepository
    ├── IPricingTierRepository
    ├── ITicketRepository
    └── IUnitOfWork
```

Infrastructure provides the concrete implementations:

```text
Infrastructure
    │
    ├── EventRepository
    ├── PricingTierRepository
    ├── TicketRepository
    └── TicketingDbContext
```

This allows application services to be unit tested without requiring a real database.

---

## Database-Calculated Ticket Total

`Ticket.TotalAmount` is calculated by SQL Server:

```text
Quantity × UnitPrice
```

This provides a database-level source of truth for the stored ticket total.

The application therefore does not calculate `TotalAmount` in the `Ticket` constructor.

---

## Transactional Ticket Purchase

Ticket purchasing is executed through the Unit of Work transaction mechanism.

The purchase operation coordinates inventory changes and ticket creation within the same transaction boundary.

This helps ensure that partial updates are not committed when the overall purchase operation fails.

---

## Inventory Protection

The ticket purchasing workflow validates available inventory before creating a ticket.

Pricing tier inventory is updated as part of the purchase operation, helping prevent tickets from being sold beyond the configured availability.

---

# Error Handling

The API uses centralized exception handling middleware.

Application/domain exceptions can be translated into appropriate HTTP responses rather than having each controller implement its own exception-handling logic.

This keeps controllers focused on HTTP/API concerns.

---
# Architectural Trade-offs

The solution deliberately favours simplicity, maintainability and correctness while avoiding unnecessary complexity for the scope of the exercise.

### Explicit Repositories vs Generic Repository

I considered using a generic repository to reduce repetitive CRUD code. However, I chose explicit repositories such as `EventRepository`, `TicketRepository` and `PricingTierRepository`.

The ticketing domain has different querying and persistence requirements for each entity. Explicit repositories make these responsibilities clearer and allow queries to be optimised independently.

**Trade-off:** This introduces some additional code compared with a generic repository, but provides better control and clearer domain-specific data access.

### Service Layer vs CQRS/MediatR

I considered introducing CQRS and MediatR to separate commands and queries. However, given the size and scope of this application, I considered the additional abstractions unnecessary.

The application/service layer provides sufficient separation between controllers, business logic and persistence while keeping the solution easier to understand and maintain.

**Trade-off:** The current design provides less formal separation between commands and queries, but avoids unnecessary complexity. CQRS/MediatR could be introduced later if the application grows and the complexity justifies it.

### Transactional Consistency vs Distributed Architecture

Preventing ticket overselling is a critical requirement. Therefore, the ticket purchase operation prioritises consistency and atomic database operations.

A more distributed architecture could provide additional scalability, but would introduce complexity around distributed transactions, consistency and failure handling.

**Trade-off:** The design prioritises correctness and consistency over distributed scalability, which is appropriate for the scope of this application.

### Modular Monolith vs Microservices

I considered a microservices architecture but chose a modular monolith for this exercise.

The application has clear logical boundaries, but splitting those boundaries into independent services would introduce additional network communication, deployment, monitoring and operational complexity.

**Trade-off:** A modular monolith provides simpler development and deployment while retaining clear separation of responsibilities. The boundaries can be extracted into services later if there is a genuine scalability or organisational requirement.

### Simplicity vs Future Extensibility

The solution avoids introducing infrastructure such as caching, messaging or distributed systems unless there is a demonstrated requirement for them.

This keeps the implementation focused on the current functional requirements while maintaining clear application, domain and infrastructure boundaries.

**Trade-off:** Some future scalability capabilities would need to be added later, but the current design avoids premature complexity and keeps the solution easier to maintain.


---


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