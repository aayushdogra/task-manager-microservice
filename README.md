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

`Data/` will later include:

- EF Core DbContext
- Migrations
- SQL schema

### ✔ Health check endpoint

`/health` → returns status for uptime monitoring.

### ✔ Task CRUD operations (upcoming)

- `GET /tasks`
- `GET /tasks/{id}`
- `POST /tasks`
- `PUT /tasks/{id}`
- `DELETE /tasks/{id}`

### ✔ Docker-ready project (planned)

Will include a `Dockerfile` + `docker-compose.yml`.

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
│   └── (data files later)
├── README.md
└── TaskManager.csproj
```
