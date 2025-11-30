# Task Manager Microservice (Minimal API — .NET 10)

A lightweight, production-style **Task Manager microservice** built using **.NET 10 Minimal APIs**.  
This project demonstrates clean architecture, endpoint grouping, service-layer separation, and container-ready deployment.

This service exposes REST APIs for:

- Creating tasks
- Fetching tasks
- Updating tasks
- Deleting tasks
- Health monitoring (`/health`)

---

## 🚀 Features

### ✔ Minimal API (no controllers, clean & fast)

### ✔ Organized folder structure

- `/Models`
- `/Services`
- `/Endpoints`
- `/Data`

### ✔ In-memory repository (no DB required initially)

Used for rapid local development.

### ✔ Ready for real database (SQL) integration

- EF Core packages installed
- `TasksDbContext` added
- Initial SQL schema defined in `Data/TasksTable.sql`
- PostgreSQL integration prepared (via Docker)

### ✔ Health check endpoint

`/health` → returns status for uptime monitoring.

### ✔ Full Task CRUD (Completed)

- `GET /tasks`
- `GET /tasks/{id}`
- `POST /tasks`
- `PUT /tasks/{id}`
- `DELETE /tasks/{id}`

### ✔ Docker-ready project (in progress)

- `docker-compose.yml` (PostgreSQL service)  
- Application Dockerfile (planned)

---

## 🐳 Docker (PostgreSQL)

To start the PostgreSQL database locally:

```bash
docker compose up -d

---

## 📅 Next Milestone (WIP)

- Connect PostgreSQL using EF Core
- Implement DbTaskService with real persistence
- Add CreatedAt / UpdatedAt timestamps
- Add Application Dockerfile
- Add environment-based configuration

---

## 📁 Project Structure

```txt
TaskManager/
├── Program.cs
├── Models/
│   └── TaskItem.cs
├── Endpoints/
│   ├── HealthEndpoints.cs
│   └── TaskEndpoints.cs
├── Services/
│   ├── ITaskService.cs
│   └── InMemoryTaskService.cs
├── Data/
│   ├── TasksDbContext.cs
│   └── TasksTable.sql
├── docker-compose.yml
├── README.md
└── TaskManager.csproj
```