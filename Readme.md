# SwiftDrop: Parcel Delivery & Courier Management System

SwiftDrop is an enterprise parcel delivery platform. Customers and merchants book and track parcels, dispatchers assign deliveries, and couriers update progress in real time. Merchants integrate through a secured API and receive status webhooks.

Built for the Enterprise Application Development group assignment (group of 10).

## Table of Contents

## Features

| Role               | Capabilities                                                                                                                                                |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Customer           | Sign up and log in (including Google OAuth 2.0), create delivery requests, see fee estimate, real-time tracking, history, PDF receipt, public tracking page |
| Merchant           | Registration with admin approval, API key or OAuth client credentials, create orders via API, webhooks with retry, orders dashboard                         |
| Courier            | View assigned deliveries, update status, upload proof, report failed deliveries                                                                             |
| Dispatcher / Admin | Assign parcels, monitor courier workload, manage users, merchants, zones and pricing, view reports                                                          |
| Bonus              | Kubernetes deployment, AI delivery assistant with MCP tools **(planned)**                                                                                   |

## Architecture

SwiftDrop is a **modular monolith**: one deployable ASP.NET Core application split into ten business modules, each owning its own database schema.

- **Three tiers inside every module:** Presentation (`Api`), Business (`Business`), Data (`Data`).
- **Contracts project per module:** the only assembly other modules may reference.
- **Synchronous calls:** in-process interfaces from `Contracts` (for example, `IPricingService`).
- **Asynchronous integration:** RabbitMQ publish/subscribe for status events, consumed by Webhooks, Notifications, Realtime, and Reporting.
- **Real-time updates:** SignalR (WebSockets).

### Modules

| Module        | Responsibility                                             |
| ------------- | ---------------------------------------------------------- |
| Identity      | Signup, login, JWT, Google OAuth, roles                    |
| Merchants     | Registration, approval, API keys, OAuth clients            |
| Pricing       | Zones and fee calculation (Strategy pattern)               |
| Parcels       | Booking, tracking number, status lifecycle, history, proof |
| Dispatch      | Courier assignment and workload                            |
| Webhooks      | Merchant callbacks with retry                              |
| Notifications | Email / mock SMS consumer                                  |
| Realtime      | SignalR live tracking                                      |
| Reporting     | Deliveries per day, success/failure rate                   |
| AIAssistant   | Chatbot and MCP tools (bonus)                              |

### Module rules

1. Reference another module only through its `*.Contracts` project.
2. One `DbContext` and one schema per module; no cross-module joins or foreign keys.
3. Controllers call services only, and never return entities (DTOs only).
4. Implementation classes are `internal`; only interfaces and `AddXModule()` extensions are public.
5. Cross-module reactions go through RabbitMQ events.

## Tech Stack

| Layer      | Technology                                                             |
| ---------- | ---------------------------------------------------------------------- |
| Frontend   | React, TypeScript, Vite, React Router, React Query, SignalR client     |
| Backend    | ASP.NET Core (.NET 10), Web API controllers, EF Core, FluentValidation |
| Database   | PostgreSQL (SQLite for local development)                              |
| Messaging  | RabbitMQ                                                               |
| Auth       | JWT, Google OAuth 2.0, API keys, OAuth client credentials              |
| API docs   | OpenAPI + Swagger UI                                                   |
| Testing    | xUnit, Moq, FluentAssertions, Testcontainers, NetArchTest, Coverlet    |
| Deployment | Docker, docker-compose, Kubernetes **(planned)**                       |

## Repository Structure

```
SwiftDrop/
├── backend/
│   ├── SwiftDrop.slnx
│   ├── scripts/init.sh              solution generation script
│   ├── src/
│   │   ├── Host/SwiftDrop.Host      runnable app, composition root
│   │   ├── SharedKernel/            base types, messaging abstractions, middleware
│   │   └── Modules/<Module>/
│   │       ├── SwiftDrop.<Module>.Contracts   public boundary
│   │       ├── SwiftDrop.<Module>.Api         controllers, DTOs, AddXModule()
│   │       ├── SwiftDrop.<Module>.Business    services, validators, rules
│   │       └── SwiftDrop.<Module>.Data        DbContext, repositories, messaging
│   └── tests/                       unit, integration, architecture tests
├── frontend/swiftdrop-frontend/     React SPA
├── k8s/                             Kubernetes manifests (planned)
├── docs/                            design documents, diagrams, test reports
├── docker-compose.yml               (planned)
└── .env.example
```

## Prerequisites

- .NET SDK 10.0 or later (`dotnet --version`)
- Node.js 20+ and npm
- Docker Desktop (for PostgreSQL, RabbitMQ, and integration tests)
- Git

## Getting Started

### Clone

```bash
git clone <repository-url>
cd SwiftDrop
```

Edit `.env` and set your own values (see [Configuration](#configuration)).

### Run the backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/Host/SwiftDrop.Host
```

In development each module creates its own SQLite file (for example `pricing.db`) next to the Host and seeds sample data.

### Demo logins (Development only)

The Identity module seeds these accounts into `identity.db` when the app runs in the Development environment.

| Role               | Username    | Password    |
| ------------------ | ----------- | ----------- |
| Dispatcher / Admin | `admin`     | `admin`     |
| Customer           | `customer1` | `customer1` |
| Courier            | `courier1`  | `courier1`  |
| Merchant           | `merchant1` | `merchant1` |

Log in with `POST /api/v1/auth/login` in Swagger (http://localhost:5017/swagger), then paste the `accessToken` into **Authorize**.

## Configuration

| Environment variable | Purpose                                                                                                     |
| -------------------- | ----------------------------------------------------------------------------------------------------------- |
| `Jwt__Key`           | Secret that signs login tokens, at least 32 bytes. Required outside Development; generated at startup in Development. |

### Run the frontend

```bash
cd frontend/swiftdrop-frontend
npm install
npm run dev
```

Open the URL printed by Vite (normally http://localhost:5173).

> If `npm install` fails with an `ERESOLVE` error about TypeScript and `typescript-eslint`, install a compatible TypeScript: `npm install -D typescript@~5.9`.

Test reports and the coverage summary are stored in `docs/`.

## API Documentation

- Swagger UI: `/swagger`
- OpenAPI spec: `/openapi/v1.json`
- All endpoints are versioned under `/api/v1`.
