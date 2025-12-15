# Task Manager Microservice (Minimal API — .NET 10)

A production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**, PostgreSQL, EF Core, FluentValidation, 
Serilog, and clean architectural practices.  

This project demonstrates:

- Minimal API–based design (no controllers)
- Clean separation of concerns (Endpoints → Services → Data)
- DTO-based API contracts 
- Centralized request validation (FluentValidation)
- Service-layer abstraction
- Sorting + filtering + pagination
- Stateless JWT authentication (Phase 1)
- PostgreSQL-backed persistence (EF Core)
- Environment-based configuration  
- Structured logging + global exception handling  
- Debug & health endpoints for development & diagnostics  

This service exposes REST APIs for:

- Creating tasks
- Fetching paginated, filtered & sorted tasks
- Updating tasks
- Deleting tasks
- Health & DB monitoring (`GET /health`, `GET /db-health`)
- Debugging endpoints (`GET /db-tasks-count`, `POST /db-test-task`, `GET /debug/tasks`)
- User registration, User login, JWT token issuance (Phase 1)

---

## Features

### Minimal API (no controllers)

Lightweight, fast, and clean endpoint definitions using .NET 10 Minimal API style, avoiding MVC overhead while retaining 
structure and clarity.

### Organized folder structure

- `/Models` — database entities  
- `/Dto` — API request/response contracts
- `/Validators` — FluentValidation validators
- `/Services` — business logic (abstraction + implementations)
- `/Endpoints` — grouped API endpoint mappings 
- `/Helpers` - shared helper & extension logic
- `/Data` — EF Core DbContext + SQL schema 
- `/logs` — Serilog rolling log files

### DTO-based API contracts

All endpoints use **CreateTaskRequest**, **UpdateTaskRequest**, **TaskResponse** and **PagedResponse<T>** DTOs
for clean separation between database models and public API responses and to prevent leaking internal DB structures.

### Request Validation (FluentValidation)

Centralized, enterprise-grade request validation using FluentValidation.
- Separate validators for create & update requests
- No manual validation logic in endpoints
- Consistent `400 Bad Request` responses
- Structured validation error format (ProblemDetails)

Validation Rules
- Title: required, 2–100 characters
- Description: optional, max 500 characters

Validation is executed explicitly in Minimal API endpoints via dependency injection.

### PostgreSQL-backed persistence (EF Core)

- Real database-backed CRUD via `DbTaskService` 
- EF Core used strictly inside the service layer
- Endpoints are DB-agnostic
- Fully persistent task creation, updates, and deletions
- Tracks `CreatedAt` and `UpdatedAt` timestamps
- InMemoryTaskService removed from DI (can be used for tests only)

### Sorting, Filtering, and Pagination
Supported features:
- Filter by completion status (`isCompleted`)
- Sort by: `CreatedAt`, `UpdatedAt`, `Title`  
- Direction: `Asc` / `Desc` 
- Enum-based strict validation for sort fields
- Stable secondary sorting (`Id`)  
- Page clamping for invalid pages  
- Maximum page size enforcement (safety)

Pagination, sorting, and filtering logic is handled entirely in the **service layer**, 
keeping endpoints thin and focused on HTTP concerns only.

### JWT Authentication (Phase 1)

Stateless JWT authentication is implemented to support user registration and login.

- `POST /auth/register` — register a new user
- `POST /auth/login` — authenticate user and issue JWT access token
- Password hashing using `PasswordHasher<T>`
- JWT generation using HS256
- Token claims include `sub`, `email`, `jti`, and `expiration`
- Authentication and authorization middleware configured in correct order
- No refresh tokens or role-based authorization in this phase

This phase focuses on establishing a clean and correct authentication foundation.

### Health monitoring & Debugging

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

### Structured Logging (Serilog)

- Centralized logging using Serilog  
- Console + rolling file logs (`logs/log-*.txt`)  
- Supports production overrides and environment-based logging levels  

### Global Exception Handling

- Automatic 500 error handling  
- Logs all unhandled exceptions with stack traces 
- Prevents stack trace leakage in production
- Returns clean JSON error responses:

```json
{
  "error": "An unexpected error occurred.",
  "details": "Optional message (dev only)"
}
```

### Docker-ready (database)

- Includes `docker-compose.yml` for running PostgreSQL locally  
- API Dockerfile planned next

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
- Implemented **Pagination, Filtering, Sorting** with page clamping
- Added enum-based sorting (CreatedAt, UpdatedAt, Title)
- Added Strict validation for `sortBy` / `sortDir`
- Implemented **secondary sorting (Id)** to ensure stable results
- Added `PagedResponse<T>` with metadata
- Added FluentValidation for create & update requests
- Removed manual validation logic from endpoints
- Centralized validation error handling via extensions

### Authentication
- Added user registration (`POST /auth/register`) and login (`POST /auth/login`) endpoints
- Implemented password hashing using `PasswordHasher<T>`
- Implemented JWT access token generation and validation
- Configured authentication and authorization middleware
- Verified token issuance and validation flow

### Infrastructure & Stability
- Added Serilog structured logging (console + rolling file logs under `/logs`)
- Added global exception handling middleware for clean error responses
- Added configuration system using `appsettings.json` + `appsettings.Development.json`
- Added new **Debug Endpoint:** `/debug/tasks?take=5` (uses shared sorting helper)

---

## Next Milestone (WIP)

### High Priority (Core Backend Features)

- Add **Application Dockerfile** (containerize the API)
- Add **Redis caching** for GET-heavy endpoints
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
│   ├── TaskItem.cs
│   └── User.cs
│
├── Dto/
│   ├── Auth/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── AuthResponse.cs
│   ├── CreateTaskRequest.cs
│   ├── UpdateTaskRequest.cs
│   ├── TaskResponse.cs
│   ├── PagedResponse.cs
│   └── SortOptions.cs
│
├── Validators/
│   ├── CreateTaskRequestValidator.cs
│   └── UpdateTaskRequestValidator.cs
│
├── Endpoints/
│   ├── TaskEndpoints.cs
│   ├── AuthEndpoints.cs
│   └── HealthEndpoints.cs
│
├── Helpers/
│   ├── TaskSortingHelper.cs
│   └── ValidationExtensions.cs
│
├── Services/
│   ├── ITaskService.cs
│   ├── DbTaskService.cs
│   ├── InMemoryTaskService.cs
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   └── JwtTokenGenerator.cs
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

  "Jwt": {
    "Issuer": "TaskManager",
    "Audience": "TaskManagerUsers",
    "Key": "DEV_ONLY_SUPER_SECRET_JWT_KEY_123456",
    "ExpiryMinutes": 60
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