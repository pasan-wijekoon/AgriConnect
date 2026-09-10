# AgriConnect

**Smart Agriculture Marketplace & Advisory Platform** — connecting Sri Lankan smallholder farmers directly with buyers, with an AI advisory layer that every price, match, and delivery schedule must pass through a human officer before it takes effect.

---

## The Problem

Smallholder farmers typically sell through several layers of middlemen before produce reaches a buyer. That chain depresses farm-gate prices, inflates what buyers pay, hides fair market pricing, leaves quality standards inconsistent, and gives collection centres no reliable audit trail of listings, inspections, or orders.

## The Solution

AgriConnect removes the unnecessary middlemen while keeping collection centres in their essential role of quality control and logistics coordination:

- **Direct listings & orders** — farmers list produce, buyers order directly against it.
- **AI-assisted fair pricing** — an advisory service analyses recent market data and proposes a price per listing.
- **Human-approved decisions, always** — every AI-suggested price, buyer–farmer match, or delivery schedule must be approved, rejected, or revised by a collection-centre officer before it becomes final.
- **Standardised quality verification** — officers inspect and confirm quality grades before a listing goes live.
- **Transparent market data** — price trends and shortage/oversupply patterns are surfaced to officers and admins.
- **Full audit trail** — every listing, inspection, order, and approval decision is recorded.

---


## Features (as specified)

- **Role-based access** for Farmer, Buyer, Collection-Centre Officer, and Administrator
- **Produce listings** — create, edit, withdraw, search, filter, sort, and paginate
- **AI-suggested fair pricing** for every new listing, grounded in recent market data
- **Quality grading & inspection**, with flagging when claimed vs. inspected grade differs
- **Orders & logistics** — stock reservation (safe under concurrent orders), conflict-free pickup/delivery scheduling, nearest-collection-centre recommendation and distance estimate
- **Agentic AI approval workflow** — every AI proposal (price, match, or schedule) goes through Approve / Reject / Request Revision by an officer, with the outcome logged
- **Market analytics** — price trend history, fair-price deviation flags, shortage/oversupply detection, exportable summary reports
- **Notifications** for farmers and buyers as listings and orders progress
- **Full audit logging** of listings, inspections, orders, AI proposals, and approval decisions

See `documentation/AgriConnect_SRS.md` for the full functional and nonfunctional requirements, and `documentation/AgriConnect_DFD.md` for the design specification.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Mobile (Farmer/Buyer) | Flutter — Android, iOS, macOS, Windows, Linux, Web |
| Web (Officer/Admin) | React 19 + TypeScript + Vite |
| Backend API | ASP.NET Core Web API (.NET 10) |
| Agentic AI service | Python + FastAPI, orchestrated with LangChain / LangGraph |
| Database | PostgreSQL, accessed via EF Core |
| Maps & distance | OpenRouteService API |
| Hosting | Render (API, PostgreSQL, Agentic AI service) · Vercel (React web app) |

**Architecture in short:** both clients (Flutter and React) talk only to the ASP.NET Core API — never directly to PostgreSQL, the Agentic AI service, or any third-party service. The API is the single source of truth for identity, authorization, validation, and persistence. It invokes the Agentic AI service internally with an objective and gets back a structured proposal; the AI service never writes to the database itself, and every proposal it produces waits for an officer's decision before anything is committed.

---

## Repository Structure

```
AgriConnect/
├── backend/          # ASP.NET Core Web API — auth, business logic, PostgreSQL access
│   └── src/
│       ├── controllers/
│       ├── services/
│       ├── models/
│       ├── dtos/
│       └── config/
├── web/               # React + TypeScript + Vite — officer/admin dashboard
│   └── src/
│       ├── components/
│       ├── pages/
│       ├── context/
│       └── utils/
├── mobile/            # Flutter app — farmer & buyer experience
│   └── lib/
│       ├── models/
│       ├── providers/
│       ├── screens/
│       ├── services/
│       ├── widgets/
│       └── utils/
├── agentic-ai/        # Python FastAPI service — LangGraph agent orchestration (internal only)
│   └── src/app/
│       ├── agents/
│       ├── orchestration/
│       ├── tools/
│       ├── validation/
│       ├── persistance/
│       ├── observability/
│       └── api/
├── docker/            # Dockerfiles & Compose configurations for all services
│   ├── backend/
│   ├── agentic-ai/
│   ├── web/
│   ├── docker-compose.yml
│   └── docker-compose.dev.yml
├── docker-compose.yml # Root compose wrapper
├── documentation/     # SRS and design specification
└── .github/workflows/ # CI/CD pipelines (backend, agentic-ai, frontend, Android)
```

---

## Getting Started

### Quick Start with Docker (Recommended)

Run the entire stack (ASP.NET Core API, Agentic AI service, React Dashboard, and PostgreSQL) with a single command:

```bash
# 1. Copy sample environment variables
cp docker/.env.example docker/.env

# 2. Start all services
docker compose up --build
```

#### Service Endpoints:
- **Web Dashboard**: [http://localhost:3000](http://localhost:3000)
- **Backend API & Swagger**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Agentic AI Docs**: [http://localhost:8000/docs](http://localhost:8000/docs)
- **PostgreSQL Database**: `localhost:5432`

> For development mode with live code reloading or service-specific logging, see [`docker/README.md`](docker/README.md).

---

### Manual Setup (Without Docker)

#### Backend — ASP.NET Core API

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend
dotnet restore
dotnet run
```

The base URL used for local testing requests is configured in `backend/backend.http` (`http://localhost:9000` by default) — adjust it to match the port .NET prints on startup. Configure PostgreSQL and any secrets in `appsettings.Development.json`, which is git-ignored.

#### Agentic AI service — FastAPI + LangGraph

Requires Python 3.12+ and [uv](https://docs.astral.sh/uv/).

```bash
cd agentic-ai
uv sync
cp .env.example .env   # fill in LLM_PROVIDER / API keys, or leave LLM_PROVIDER=mock
uv run uvicorn src.app.main:app --reload --port 8000
```

This service is internal-only: it's meant to be called by the ASP.NET Core API, not directly by the mobile or web clients.

#### Web — React + TypeScript + Vite

Requires Node.js.

```bash
cd web
npm install
npm run dev
```

#### Mobile — Flutter

Requires the [Flutter SDK](https://docs.flutter.dev/get-started/install).

```bash
cd mobile
flutter pub get
flutter run
```

Platform targets are available via `flutter run -d <android|ios|macos|windows|linux|chrome>`.

---

## Documentation

- [`documentation/AgriConnect_SRS.md`](documentation/AgriConnect_SRS.md) — Software Requirements Specification: problem statement, stakeholders, scope, functional & nonfunctional requirements.
- [`documentation/AgriConnect_DFD.md`](documentation/AgriConnect_DFD.md) — Design Specification: architecture, component design, agent orchestration, database design, deployment plan, and requirements traceability.

---

## CI/CD

Workflow files exist under `.github/workflows/` for the backend, the agentic AI service, the frontend, and Android builds; pipelines are scaffolded and not yet populated.

## Deployment (planned)

Per the design specification: the ASP.NET Core API, PostgreSQL, and the Agentic AI service are deployed on **Render** (the Agentic AI service as a private, internal-only instance reachable only by the API), while the **React** web app is deployed on **Vercel**. The Flutter app ships as a distributable Android APK rather than a hosted service.

## License

See the [LICENSE](LICENSE) file.
