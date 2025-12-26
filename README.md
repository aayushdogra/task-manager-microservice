# Task Manager Microservice (Minimal API - .NET 10)

A production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**, PostgreSQL, EF Core, 
FluentValidation, Serilog, and clean architectural practices.  

## Why this project?
This project is built to demonstrate how a **real-world backend service** is designed, 
structured, and productionized using modern backend engineering practices.

The goal is not just CRUD functionality, but to show:
•	clean architecture
•	separation of concerns
•	scalable API design
•	authentication & authorization
•	validation
•	rate limiting
•	pagination & sorting
•	caching
•	containerization
•	production-readiness
It simulates how a **task management microservice** would be implemented in a real backend system.

---

This project demonstrates:

- Minimal API–based design (no controllers)
- Clean separation of concerns (Endpoints → Services → Data)
- DTO-based API contracts
- Explicit request validation for Minimal APIs
- Centralized request validation (FluentValidation)
- Service-layer abstraction
- Sorting + filtering + pagination
- Stateless JWT authentication + Database-backed refresh tokens
- Secure token lifecycle (login → refresh → logout)
- Endpoint-level authorization for write APIs
- Selective rate limiting and abuse protection
- PostgreSQL-backed persistence (EF Core)
- Environment-based configuration
- Structured logging + global exception handling
- Debug & health endpoints for development & diagnostics
- Redis-based caching for read-heavy endpoints
- Cache-aside strategy with observability via response headers
- Cache invalidation on write operations with graceful fallback
- Consistent API response envelope for success and error cases
- Structured validation errors with field-level messages

This service exposes REST APIs for:

- Creating tasks
- Fetching paginated, filtered & sorted tasks
- Updating tasks
- Deleting tasks
- User registration, User login, JWT access and refresh token issuance
- Fetching current authenticated user info (`GET /me`)
- Health & DB monitoring (`GET /health`, `GET /db-health`)
- Redis health monitoring (`GET /redis-health`)
- Debugging endpoints (`GET /db-tasks-count`, `POST /db-test-task`, `GET /debug/tasks`)

---

## Features

### Minimal API (no controllers)

Lightweight, fast, and clean endpoint definitions using .NET 10 Minimal API style, avoiding MVC overhead while 
retaining structure and clarity.

### Organized folder structure

- `/Models` — database entities  
- `/Dto` — API request/response contracts
- `/Validators` — FluentValidation validators
- `/Services` — business logic, caching abstraction + implementations
- `/Endpoints` — grouped API endpoint mappings
- `/Middleware` — cross-cutting middleware (rate limiting, exceptional handling)
- `/RateLimiting` — rate limiting logic & configuration
- `/Helpers` — shared helper & extension logic
- `/Data` — EF Core DbContext + SQL schema 
- `/logs` — Serilog rolling log files

---

### DTO-based API contracts

All endpoints use DTOs for clean separation between database models and public API responses 
and to prevent leaking internal DB structures.

All responses are wrapped in a consistent API envelope:

```json

{
  "success": true | false,
  "data": ...,
  "error": {
    "code": "ERROR_CODE",
    "message": "Message",
    "details": { }
  }
}

```

---

### Request Validation (FluentValidation)

Centralized, enterprise-grade request validation using FluentValidation.

- Separate validators for create & update requests
- No manual validation logic in endpoints
- Consistent `400 Bad Request` responses
- Structured validation error format with field-level messages

Task Validation Rules
- Title: required, 2–100 characters
- Description: optional, max 500 characters

Auth Validation Rules
- Register/Login: 
    - Email: required, valid email format
    - Password: required
- Refresh/Logout: 
  - RefreshToken: required

Validation is executed explicitly in Minimal API endpoints via dependency injection.

---

### PostgreSQL-backed persistence (EF Core)

- Real database-backed CRUD via `DbTaskService` 
- EF Core used strictly inside the service layer
- Endpoints are DB-agnostic
- Fully persistent task creation, updates, and deletions
- Tracks `CreatedAt` and `UpdatedAt` timestamps
- User-scoped task isolation enforced at query level

---

### Sorting, Filtering, and Pagination
Supported features:

- Filter by completion status (`isCompleted`)
- Sort by: `CreatedAt`, `UpdatedAt`, `Title`  
- Direction: `Asc` / `Desc` 
- Enum-based strict validation for sort fields
- Stable secondary sorting (`Id`)  
- Strict pagination validation
- Maximum page size enforcement
- Out-of-range pages return empty results

All logic is handled entirely in the **service layer**, keeping endpoints thin and focused on HTTP concerns only.
Pagination navigation links are exposed via standard `HTTP Link headers (self, next, prev)`.

---

### Redis Caching (Read Path Optimization)
Redis is used to optimize read-heavy task queries using a cache-aside strategy.

- Implemented for `GET /tasks`
- User-scoped caching to prevent data leakage
- Cache keys include pagination, sorting, and filters
- Cached values store **final DTO responses**, not EF entities
- Short TTL used to balance performance and freshness
- Redis integration is isolated to the service layer
- Caching is treated as an **implementation detail**, not part of the service contract

Cache key format: `tasks:{userId}:{queryHash}`

---

### Cache Invalidation (Write Path)
To ensure consistency between cache and database:

- Cache entries are invalidated on:
    - `POST /tasks`
    - `PUT /tasks/{id}`
    - `DELETE /tasks/{id}`
- Full user-scoped invalidation is used to keep pagination safe
- Redis failures are handled gracefully without impacting writes
- Database remains the source of truth

**Cache Observability**

Cache behavior is exposed via HTTP response headers:

`X-Cache: HIT | MISS`

---

### JWT Authentication

Stateless JWT authentication is implemented to support user registration and login.

- `POST /auth/register`
- `POST /auth/login`
- Password hashing using `PasswordHasher<T>`
- JWT generation using `HS256`
- Token claims include `nameidentifier (UserId)`, `email`, `jti`, and `expiration`

---

### Refresh Tokens

Database-backed refresh tokens are implemented to support long-lived authentication.

- `POST /auth/refresh` — issue a new access token using a valid refresh token
- Refresh tokens are stored in PostgreSQL
- Expiration and revocation supported
- Access tokens remain stateless and short-lived
- Invalid, expired, or revoked refresh tokens return `401 Unauthorized`

---

### Current User Endpoint (`/me`)

A dedicated endpoint is provided to fetch the currently authenticated user’s profile information.

- `GET /me` returns authenticated user info
- Data fetched from DB to ensure consistency
- Protected via `.RequireAuthorization()`

---

### Logout Semantics
- `POST /auth/logout` revokes refresh tokens
- Access tokens remain valid until expiry
- Logout is idempotent (safe to call multiple times)

---

### Authorization (API Protection)
Authorization is enforced at endpoint level using Minimal API metadata.

**Protected Endpoints include:**
- `GET /tasks`
- `GET /tasks/{id}`
- `POST /tasks`
- `PUT /tasks/{id}`
- `DELETE /tasks/{id}`
- `POST /db-test-task`
- `GET /me`
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`

---

### Rate Limiting & Abuse Protection

- In-memory fixed window rate limiting
- Configured as **100 requests / 10 minutes**
- Middleware-based implementation
- Applied selectively using endpoint metadata
- Supports **IP-based and per-user limiting**
- Returns `429 Too Many Requests`
- Includes:
    - `X-RateLimit-Limit`
    - `X-RateLimit-Remaining`

---

### Health monitoring & Debugging

- `/health` service liveness 
- `/db-health` PostgreSQL connectivity
- `/redis-health` Redis connectivity
- `/db-tasks-count`debugging DB visibility 
- `/db-test-task` inserts test task
- `/debug/tasks?take=N` — fetch top N sorted tasks

---

### Structured Logging (Serilog)

- Centralized logging using Serilog  
- Console + rolling file logs (`logs/log-*.txt`)  
- Environment-based configuration
- Safe production logging defaults

---

### Global Exception Handling

- Centralized exception handling middleware
- Maps domain errors to HTTP status codes (`400`, `401`, `500`)
- Logs all unhandled exceptions with stack traces 
- Prevents stack traces from leaking to clients
- Returns clean JSON error responses:

```json
{
    "success": true | false,
    "data": ...,
    "error": {
        "code": "ERROR_CODE",
        "message": "Message",
        "details": { }
    },
    "meta": null
}
```

---

## 🐳 Docker (API + PostgreSQL + Redis)
The project is fully containerized and can be run using **Docker Compose**, including:

- API service
- PostgreSQL database
- Redis cache
- Health checks
- Environment-based configuration

Run everything locally:

```bash
docker compose up --build 
```
This starts:
- Task Manager API
- PostgreSQL
- Redis

---

### API service
- Base URL: `http://localhost:8080`
- Health: `GET /api/v1/health`
- Database health: `GET /api/v1/db-health`
- Redis health: `GET /api/v1/redis-`health`

Docker health checks ensure the API is marked healthy only after startup completes.

---

### Environment configuration (`.env`)
The project supports environment-based configuration via a .env file.

```env

ASPNETCORE_ENVIRONMENT=Docker

POSTGRES_DB=tasks_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

CONNECTIONSTRINGS__TASKSDB=Host=postgres;Port=5432;Database=tasks_db;Username=postgres;Password=postgres

REDIS__CONNECTIONSTRING=redis:6379

```

---

### Docker health checks
Each container defines its own health check:

- API → `/api/v1/health`
- PostgreSQL → `pg_isready`
- Redis → `redis-cli ping`

Verify health status:
```bash
docker ps
```

Expected output:
```txt
Up (healthy)
```

---

## Project Structure

```txt

TaskManager/
├── Program.cs
├── appsettings.json
│
├── Dockerfile
├── docker-compose.yml
├── README.md
│
├── Models/
│   ├── TaskItem.cs
│   ├── User.cs
│   └── RefreshToken.cs
│
├── Dto/
│   ├── ApiResponse.cs
│   ├── Auth/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── AuthResponse.cs
│   │   ├── RefreshRequest.cs
│   │   └── MeResponse.cs
│   ├── CreateTaskRequest.cs
│   ├── UpdateTaskRequest.cs
│   ├── TaskResponse.cs
│   ├── PagedResponse.cs
│   └── SortOptions.cs
│
├── Validators/
│   ├── CreateTaskRequestValidator.cs
│   ├── UpdateTaskRequestValidator.cs
│   ├── RegisterRequestValidator.cs
│   ├── LoginRequestValidator.cs
│   └── RefreshRequestValidator.cs
│
├── Endpoints/
│   ├── TaskEndpoints.cs
│   ├── AuthEndpoints.cs
│   └── HealthEndpoints.cs
│
├── Middleware/
│   └── RateLimitingMiddleware.cs
│
├── RateLimiting/
│   ├── RateLimitOptions.cs
│   ├── RateLimitEntry.cs
│   ├── InMemoryRateLimitStore.cs
│   ├── RequireRateLimitingAttribute.cs
│   └── RequireUserRateLimitingAttribute.cs
│
├── Helpers/
│   ├── ApiResults.cs
│   ├── PaginationHelper.cs
│   ├── TaskSortingHelper.cs
│   ├── UserClaimsExtensions.cs
│   └── ValidationExtensions.cs
│
├── Services/
│   ├── ITaskService.cs
│   ├── DbTaskService.cs
│   ├── InMemoryTaskService.cs
│   ├── ICacheService.cs
│   ├── RedisCacheService.cs
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
└── TaskManager.csproj

```

---