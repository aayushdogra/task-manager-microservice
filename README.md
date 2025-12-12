# Task Manager Microservice (Minimal API — .NET 10)

A production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**, PostgreSQL, and EF Core.  
The project demonstrates clean architecture, endpoint grouping, DTO-based API contracts, service-layer abstraction, and container-ready development workflows.

This service exposes REST APIs for:

- Creating tasks
- Fetching tasks
- Updating tasks
- Deleting tasks
- Health monitoring (`/health`, `/db-health`)

---

## Features

### Minimal API (no controllers)

Lightweight, fast, and clean endpoint definitions using .NET 10 Minimal API style.

### Organized folder structure

- `/Models` — database entities  
- `/Dto` — request/response objects used by API  
- `/Services` — business logic + abstractions  
- `/Endpoints` — endpoint mappings grouped by domain  
- `/Data` — EF Core DbContext + SQL schema  

### DTO-based API contracts

All endpoints use **CreateTaskRequest**, **UpdateTaskRequest**, and **TaskResponse**  
for clean separation between database models and public API responses.

### PostgreSQL-backed persistence (EF Core)

- Real database CRUD implemented in `DbTaskService`  
- Fully persistent task creation, updates, and deletions  
- InMemoryTaskService removed from DI (can be used for tests only)

### Health monitoring

- `/health` — service health  
- `/db-health` — PostgreSQL connectivity  
- `/db-tasks-count` — useful for debugging DB reads/writes 

### Full Task CRUD (Completed)

- `GET /tasks`
- `GET /tasks/{id}`
- `POST /tasks`
- `PUT /tasks/{id}`
- `DELETE /tasks/{id}`

### Docker-ready project (database)

- Includes `docker-compose.yml` for running PostgreSQL locally  
- API Dockerfile planned next

### Structured Logging (Serilog)

- Centralized logging using Serilog  
- Console + rolling file logs (`logs/log-*.txt`)  
- Supports production overrides and environment-based logging levels  

### Global Exception Handling

- Automatic 500 error handling  
- Logs all unhandled exceptions with stack traces  
- Returns clean JSON error responses

---

## 🐳 Docker (PostgreSQL)

To start the PostgreSQL database locally:

```bash
docker compose up -d 
```
This launches a `tasks_db` PostgreSQL instance with:

- Host: `localhost`
- Port: `5432`
- User: `postgres`
- Password: `postgres`
- Database: `tasks_db`

API connects to the DB via EF Core using the connection string in appsettings.json or environment variables.

---

## Recent Milestones (Completed)
- Switched DI from `InMemoryTaskService` → `DbTaskService` (PostgreSQL-backed CRUD)
- Implemented full CRUD in DbTaskService (Create / GetAll / GetById / Update / Delete)
- Added timestamps (CreatedAt, UpdatedAt) and proper update tracking
- Rewrote `/tasks` endpoints to use clean DTO-based API contracts
- Added `TaskResponse` mapping for all read/write operations
- Implemented pagination + filtering + sorting for `/tasks`
- Added Serilog structured logging (console + rolling file logs under `/logs`)
- Implemented global exception handling middleware for clean error responses
- Added configuration system using appsettings.json + appsettings.Development.json
- Added PagedResponse<T> DTO for paginated API responses
- Implemented page clamping and max page size limits
- Added pagination metadata to responses
- Added enum-based sorting to GET /tasks
- Added Strict validation for SortBy/sortDir
- Added stable secondary sorting (Id) to ensure consistent results
---

## Next Milestone (WIP)

### High Priority (Core Backend Features)

- Add **Application Dockerfile** (containerize the API)
- Add **Redis caching** for GET-heavy endpoints
- Add **FluentValidation** for all input DTOs
- Add **JWT authentication + Refresh Tokens**
- Add **Rate limiting** (simple middleware)

---

### Intermediate / Microservice Expansion

- Add async processing via **RabbitMQ/Kafka**
- Introduce **background worker** for queue consumption
- Implement **idempotency + retry + DLQ** patterns

---

### Advanced / Production Quality

- Add **metrics + observability** (OpenTelemetry + basic metrics endpoints)
- Add **API versioning** routes (`/api/v1/...`, future `/api/v2/...`)
- Add **Docker Compose** for API + PostgreSQL + Redis
- Add **architecture diagrams + sequence diagrams**


---

## Project Structure

```txt

TaskManager/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
│
├── Models/
│   └── TaskItem.cs
│
├── Dto/
│   ├── CreateTaskRequest.cs
│   ├── UpdateTaskRequest.cs
│   ├── TaskResponse.cs
│   ├── PagedResponse.cs
│   └── SortOptions.cs
│
├── Endpoints/
│   ├── TaskEndpoints.cs
│   ├── HealthEndpoints.cs
│   └── DebugEndpoints.cs
│
├── Services/
│   ├── ITaskService.cs
│   ├── DbTaskService.cs
│   └── InMemoryTaskService.cs    (optional / test only)
│
├── Data/
│   ├── TasksDbContext.cs
│   └── TasksTable.sql
│
├── SystemDesign/
│   ├── API_Layer_Design.md
│   ├── API_Versioning.md
│   └── TaskService_Design_Polished.pdf
│
├── logs/                         (auto-generated by Serilog)
│   └── log-YYYYMMDD.txt          (rolling log files, one per day)
│
├── docker-compose.yml            (PostgreSQL + optional Redis)
├── README.md
└── TaskManager.csproj

```
---

## Configuration (appsettings.json)

The microservice uses **appsettings.json** for environment-based configuration.

### Example:

```json
{
  "ConnectionStrings": {
    "TasksDb": "Host=localhost;Port=5432;Database=tasks_db;Username=postgres;Password=postgres"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}

```

---