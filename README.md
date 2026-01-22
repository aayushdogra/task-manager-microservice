# Task Manager Microservice 
## (.NET 10 Minimal API | Production-style backend service)

This repository contains a **Task Manager microservice** built using **.NET 10 Minimal APIs**.
It is intentionally designed as a **backend system exercise**, not a starter template or tutorial project.

## Project Intent (Read This First)
This service was built to practice and demonstrate:
- Designing service boundaries in a Minimal API–only setup
- Keeping HTTP concerns thin while pushing logic into services
- Handling real backend concerns beyond CRUD
- Making design tradeoffs explicit and defendable

---

## Architectural Shape (High Level)

The system follows a **vertical slice–friendly layered flow**:

```text
HTTP Endpoint
   ↓
Request Validation
   ↓
Service Layer (Business + Query Logic)
   ↓
Persistence / Cache
```

Key rules enforced in code:
- Endpoints never talk to EF Core directly
- DTOs are the only public contracts
- Infrastructure (Redis, EF, JWT) is hidden behind interfaces
- Auth, rate limiting, caching are opt-in per endpoint

---

## Why Minimal APIs (and not Controllers)

This project intentionally avoids MVC Controllers to explore:
- Explicit dependency injection per endpoint
- Endpoint-level authorization & rate limiting
- Reduced framework magic
- Easier reasoning about request flow

Tradeoff:
- Less convention support
- More responsibility on the developer to stay consistent

This is deliberate.

---

## Folder Structure

```text
Endpoints/     → HTTP wiring only (routing, auth, validation)
Services/      → Business rules, queries, caching, auth logic
Dto/           → Public API contracts (never EF entities)
Models/        → Persistence models
Validators/    → Explicit request validation
Middleware/    → Cross-cutting runtime behavior
RateLimiting/  → Custom rate limit implementation
Helpers/       → Small, testable utility logic
Data/          → EF Core + schema ownership
```

If logic grows inside `Endpoints`, that’s a bug.

---

## API Response Model (Intentional Verbosity)

All responses are wrapped in a consistent envelope:

```json
{
  "success": true,
  "data": {},
  "error": null
}
```

Why:
- Makes error handling predictable for clients
- Forces explicit success/failure handling
- Simplifies logging & debugging

This is not free - it adds payload overhead - but consistency wins here.

---

## Validation Strategy (Explicit, Not Magical)
- FluentValidation is used
- Validators are injected manually per endpoint
- No model-binding side effects
- Validation failures are returned as structured errors

Reason:
- Minimal APIs don’t give you automatic validation pipelines.
- This project embraces that explicitness instead of hiding it.

---

## Persistence Layer (EF Core)

- EF Core is used **only inside services**
- No lazy loading
- Queries are explicitly composed
- User scoping is enforced at query level (not post-filtering)

This avoids:
- Accidental N+1 queries
- Data leakage across users
- Tight coupling between HTTP and DB

---

## Sorting, Filtering, Pagination (Realistic, Not Fancy)

Supported:
- Filtering by completion state
- Enum-validated sorting fields
- Directional sorting
- Page size limits
- Stable ordering (secondary key)

Design choice:
Pagination logic lives in the service, not helpers or middleware, because:
- It’s domain-specific
- It interacts with caching & query 

---

## Redis Caching (Read Path Only)

Caching is intentionally conservative:
- Only applied to `GET /tasks`
- Cache-aside pattern
- User-scoped keys
- DTOs cached, not entities
- Short TTLs

Invalidation strategy:
- Full user-scope invalidation on writes
- Safe but inefficient - chosen intentionally to avoid stale pagination bugs

If Redis fails:
- Reads fall back to DB
- Writes never fail because of cache

---

## Authentication Model

- JWT access tokens (short-lived, stateless)
- Refresh tokens stored in DB
- Token rotation supported
- Logout revokes refresh tokens only

Tradeoff:
Access tokens cannot be force-revoked - accepted by design.

---

## Authorization Strategy

Authorization is enforced explicitly per endpoint.

There is no global “magic policy”.

If an endpoint is protected, it says so in code.

---

## Rate Limiting (Custom, Not Built-In)

Why custom?
- To understand mechanics
- To control per-endpoint behavior
- To support both IP and user-based limits

Implementation:
- Fixed window
- In-memory store
- Metadata-driven enforcement

This is not **horizontally scalable** - intentionally accepted.

---

## Observability

- Structured logging via Serilog
- Global exception handling
- Health endpoints for API, DB, Redis
- Cache HIT/MISS exposed via headers

This service is debuggable without attaching a debugger.

---

## Docker & Local Runtime

The service is designed to run **only via Docker Compose** in realistic conditions:
- API
- PostgreSQL
- Redis

If something fails locally, it should fail the same way in prod.

```bash 
docker compose up --build
```

---