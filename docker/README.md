# AgriConnect Docker Infrastructure

This directory contains the complete Docker and Docker Compose setup for containerizing and running the entire AgriConnect platform locally or in CI/CD environments.

---

## 📦 Services Overview

| Service | Technology | Internal Port | Host Port | Description |
|---|---|---|---|---|
| **`backend`** | ASP.NET Core (.NET 10) | `5000` | `5000` | Main Web API & business logic |
| **`agentic-ai`** | Python 3.12 + FastAPI + LangGraph | `8000` | `8000` | Internal Agentic AI orchestration service |
| **`web`** | React 19 + TypeScript + Vite + Nginx | `80` | `3000` | Officer/Admin web dashboard |
| **`postgres`** | PostgreSQL 17 (Alpine) | `5432` | `5432` | Relational database storage |

---

## 🚀 Quick Start

### 1. Prerequisites
- [Docker](https://docs.docker.com/get-docker/) (v24.0+)
- [Docker Compose](https://docs.docker.com/compose/) (v2.20+)

### 2. Configure Environment Variables
Copy `.env.example` to `.env` inside the `docker/` folder or at the repository root:

```bash
# From repository root
cp docker/.env.example docker/.env
```

Review and adjust any API keys (e.g., `GEMINI_API_KEY`, `OPENAI_API_KEY`, `MAPS_API_KEY`). By default, `LLM_PROVIDER=mock` is enabled so you can run the entire system without external API keys.

### 3. Start All Services
Run one of the following commands:

**From repository root:**
```bash
docker compose up --build
```

**Or from `docker/` folder:**
```bash
cd docker
docker compose up --build
```

To run containers in the background (detached mode):
```bash
docker compose up -d --build
```

---

## 🌐 Service Endpoints

Once running, access the services at:

- **Web Dashboard**: [http://localhost:3000](http://localhost:3000)
- **Backend API Swagger**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Backend API Root**: [http://localhost:5000](http://localhost:5000)
- **Agentic AI Interactive Docs**: [http://localhost:8000/docs](http://localhost:8000/docs)
- **PostgreSQL Database**: `localhost:5432` (User: `postgres`, Password: `postgres`, DB: `agriconnect`)

---

## 🛠️ Development Mode

To enable live source reload (hot-reloading for `agentic-ai` and live mounts):

```bash
docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml up
```

---

## 🛑 Stopping & Cleaning Up

- **Stop all services:**
  ```bash
  docker compose down
  ```

- **Stop services and remove database volumes (fresh start):**
  ```bash
  docker compose down -v
  ```

- **View logs for a specific service:**
  ```bash
  docker compose logs -f backend
  docker compose logs -f agentic-ai
  docker compose logs -f web
  docker compose logs -f postgres
  ```

---

## 📁 Directory Structure

```
docker/
├── backend/
│   ├── Dockerfile            # Multi-stage .NET 10 SDK build & runtime
│   └── .dockerignore         # Build exclusion rules
├── agentic-ai/
│   ├── Dockerfile            # Python 3.12 + uv FastAPI container
│   └── .dockerignore         # Python/cache exclusions
├── web/
│   ├── Dockerfile            # Multi-stage Node 22 build + Nginx runtime
│   ├── nginx.conf            # Nginx SPA fallback routing & security headers
│   └── .dockerignore         # Node build exclusions
├── .env.example              # Template environment variables
├── docker-compose.yml        # Production-grade multi-service compose
├── docker-compose.dev.yml    # Development overrides with volume mounts
└── README.md                 # Documentation
```
