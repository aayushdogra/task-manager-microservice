# Task Manager Microservice (Minimal API — .NET 10)

A production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**, PostgreSQL, EF Core, Serilog, and clean architectural practices.  
The project demonstrates:

- Clean separation of concerns  
- DTO-based API design  
- Service-layer abstraction  
- Sorting + filtering + pagination  
- Environment-based configuration  
- Structured logging + global exception handling  
- Debug endpoints for development & diagnostics  

This service exposes REST APIs for:

- Creating tasks
- Fetching paginated & sorted tasks
- Updating tasks
- Deleting tasks
- Health & DB monitoring (`GET /health`, `GET /db-health`)
- Debugging endpoints (`GET /db-tasks-count`, `POST /db-test-task`, `GET /debug/tasks`)

---

## Features

### Minimal API (no controllers)

Lightweight, fast, and clean endpoint definitions using .NET 10 Minimal API style.

### Organized folder structure

- `/Models` — database entities  
- `/Dto` — API request/response contracts 
- `/Services` — business logic (abstraction + implementations)
- `/Endpoints` — grouped API endpoint mappings  
- `/Data` — EF Core DbContext + SQL schema 
- `/logs` — Serilog rolling log files

### DTO-based API contracts

All endpoints use **CreateTaskRequest**, **UpdateTaskRequest**, and **TaskResponse**  
for clean separation between database models and public API responses and to prevent leaking internal DB structures.

### PostgreSQL-backed persistence (EF Core)

- Real database-backed CRUD via `DbTaskService`  
- Fully persistent task creation, updates, and deletions
- - Tracks `CreatedAt` and `UpdatedAt` timestamps
- InMemoryTaskService removed from DI (can be used for tests only)

### Sorting, Filtering, and Pagination
Supported features:
- Filter by completion status  
- Sort by: `CreatedAt`, `UpdatedAt`, `Title`  
- Direction: `Asc` / `Desc`  
- Stable secondary sorting (Id)  
- Page clamping for invalid pages  
- Maximum page size enforcement

### Health monitoring

- `/health` — service health  
- `/db-health` — PostgreSQL connectivity  
- `/db-tasks-count` — useful for debugging DB reads/writes 
- `/db-test-task` - creates a test task in the DB
- `/debug/tasks` — view top N sorted tasks (uses same sort logic as main API)

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
- Returns clean JSON error responses:

```json
{
  "error": "An unexpected error occurred.",
  "details": "Optional message (dev only)"
}
```

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

### Backend & API
- Switched DI from `InMemoryTaskService` → `DbTaskService` (PostgreSQL-backed CRUD)
- Implemented full CRUD in DbTaskService (Create / GetAll / GetById / Update / Delete)
- Added timestamps (`CreatedAt`, `UpdatedAt`)
- Rewrote `/tasks` endpoints to use clean DTO-based API contracts
- Added `TaskResponse` mapping for all endpoints
- Added **Pagination, Filtering, Sorting** with page clamping
- Added enum-based sorting (CreatedAt, UpdatedAt, Title)
- Added Strict validation for `sortBy` / `sortDir`
- Added **secondary sorting (Id)** to ensure stable results
- Implemented `PagedResponse<T>` with metadata

## Infrastructure & Stability
- Added Serilog structured logging (console + rolling file logs under `/logs`)
- Added global exception handling middleware for clean error responses
- Added configuration system using `appsettings.json` + `appsettings.Development.json`
- Added new **Debug Endpoint:** `/debug/tasks?take=5` (uses shared sorting helper)

---

## Next Milestone (WIP)

### High Priority (Core Backend Features)

- Add **Application Dockerfile** (containerize the API)
- Add **Redis caching** for GET-heavy endpoints
- Add **FluentValidation** for all input DTOs
- Add **JWT authentication + Refresh Tokens**
- Add **Rate limiting** middleware

---

### Intermediate / Microservice Expansion

- Add async processing via **RabbitMQ/Kafka**
- Add a dedicated **Background Worker** service for queue consumption
- Implement **Retry Policies, Idempotency Keys, Dead Letter Queue (DLQ)**

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
├── Helpers/
│   └── TaskSortingHelper.cs
│
├── Services/
│   ├── ITaskService.cs
│   ├── DbTaskService.cs
│   └── InMemoryTaskService.cs   
│
├── Data/
│   ├── TasksDbContext.cs
│   └── TasksTable.sql
│
├── logs/
│   └── log-YYYYMMDD.txt         
│
├── docker-compose.yml
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