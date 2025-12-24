# Task Manager Microservice (Minimal API — .NET 10)

A production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**, PostgreSQL, EF Core, 
FluentValidation, Serilog, and clean architectural practices.  

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
    "message": "Human readable message",
    "details": { }
  }
}

```

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

### PostgreSQL-backed persistence (EF Core)

- Real database-backed CRUD via `DbTaskService` 
- EF Core used strictly inside the service layer
- Endpoints are DB-agnostic
- Fully persistent task creation, updates, and deletions
- Tracks `CreatedAt` and `UpdatedAt` timestamps
- User-scoped task isolation enforced at query level

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

- Enables easy debugging and verification
- Keeps API response payload unchanged
- Avoids leaking infrastructure concerns into DTOs

### JWT Authentication

Stateless JWT authentication is implemented to support user registration and login.

- `POST /auth/register` — register a new user
- `POST /auth/login` — authenticate user and issue JWT access token
- Password hashing using `PasswordHasher<T>`
- JWT generation using `HS256`
- Token claims include `nameidentifier (UserId)`, `email`, `jti`, and `expiration`

### Refresh Tokens

Database-backed refresh tokens are implemented to support long-lived authentication.

- `POST /auth/refresh` — issue a new access token using a valid refresh token
- Refresh tokens are securely generated and stored in PostgreSQL
- Tokens include expiration and revocation support
- Access tokens remain stateless and short-lived
- Invalid, expired, or revoked refresh tokens return `401 Unauthorized`

### Current User Endpoint (`/me`)

A dedicated endpoint is provided to fetch the currently authenticated user’s profile information.

- `GET /me` — returns user info based on JWT claims
- User data is fetched from the database to ensure consistency
- Endpoint is protected via `.RequireAuthorization()`

### Logout Semantics (Important)
- POST /auth/logout — logout revokes refresh tokens only
- Access tokens remain valid until expiry (by design)
- Logout is idempotent: Invalid or already-revoked tokens still return 204 No Content

### Authorization (API Protection)

Authorization is enforced at endpoint level using Minimal API metadata.

- Read / Write endpoints require authentication
- Prevents unauthorized task creation, updates, and deletions
- Authorization is enforced via JWT middleware

**Protected Endpoints:**
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

### Rate Limiting & Abuse Protection

A middleware-based rate limiting mechanism is implemented to protect the API from abuse and brute-force attacks.

- In-memory fixed window rate limiting
- Configured as 100 requests / 10 minutes
- Implemented as middleware
- Applied selectively using endpoint metadata
- Supports **IP-based and per-user rate limiting**
- Returns `429 Too Many Requests` with a friendly JSON error
- Adds rate limit headers:
    - `X-RateLimit-Limit`
    - `X-RateLimit-Remaining`

**Rate limiting strategy:**
- Auth endpoints are rate limited by **IP**
- Authenticated write endpoints are rate limited **per user (with IP fallback)**
- Read-only, health, and debug endpoints are excluded

This design keeps middleware generic while allowing endpoints to explicitly opt into rate limiting.

### Health monitoring & Debugging

- `/health` — service health  
- `/db-health` — PostgreSQL connectivity 
- `/redis-health` — Redis connectivity and read/write validation
- `/db-tasks-count` — useful for debugging DB reads/writes 
- `/db-test-task` — creates a test task in the DB
- `/debug/tasks?take=N` — view top N sorted tasks

### Structured Logging (Serilog)

- Centralized logging using Serilog  
- Console + rolling file logs (`logs/log-*.txt`)  
- Supports production overrides and environment-based logging levels  

### Global Exception Handling

- Centralized exception handling via middleware
- Maps domain exceptions to correct HTTP status codes (`400`, `401`, `500`)
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

- Includes `docker-compose.yml` for PostgreSQL and Redis
- Supports local performance and caching tests

---

## 🐳 Docker (PostgreSQL + Redis)

To start the PostgreSQL and Redis locally:

```bash
docker compose up -d 
```
This launches the following services:

**PostgreSQL** (`tasks_db`)
- Host: `localhost`
- Port: `5432`
- User: `postgres`
- Password: `postgres`
- Database: `tasks_db`

**Redis**
- Host: `localhost`
- Port: `6379`
- Used for caching read-heavy endpoints

API connects to the DB via EF Core and Redis via StackExchange.Redis using configuration from `appsettings.json`.

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
│   ├── TaskSortingHelper.cs
│   ├── ValidationExtensions.cs
│   └── UserClaimsExtensions.cs
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
├── docker-compose.yml
├── README.md
└── TaskManager.csproj

```

---