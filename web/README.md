# AgriConnect — Web Dashboard

React 19 + TypeScript + Vite. The back-office surface for Collection-Centre Officers and
Administrators. It talks only to the ASP.NET Core API — never to the database or the AI service.

## Setup

```bash
npm install
```

```bash
cp .env.example .env
```

`VITE_API_BASE_URL` points at the API (default `http://localhost:5000`).

## Run

Start the API first (`backend/`, `dotnet run`), then:

```bash
npm run dev
```

In development there is no login yet: the sidebar's **Signed in as (dev)** switch sends
`X-Dev-Role: Officer` or `Administrator`, which the API's development-only fake sign-in accepts.

## Check

```bash
npm run build
```

```bash
npm run lint
```

```bash
npm test
```

`npm test` runs the logic tests in `tests/` with Node's built-in test runner (Node 23+ runs
TypeScript directly), so no extra test packages are needed.

## Structure

```
src/
├── App.tsx              # Shell: sidebar navigation, dev role switch, page routing
├── components/          # LineChart (SVG), Drawer (<dialog>), shared UI pieces
├── context/session.ts   # Current role and the auth headers sent with each request
├── pages/               # One file per page
└── utils/               # API client, formatting, trend/heatmap logic, hash router
tests/                   # Logic tests (node --test)
```

## Component D — Market Price Analytics (Student 4)

| Page | Route | API | Roles |
|---|---|---|---|
| Price trends | `#/price-trends` | `GET /api/analytics/price-trends`, `POST /api/analytics/snapshots/refresh` | Officer, Admin (rebuild: Admin) |
| Shortages | `#/shortages` | `GET /api/analytics/shortages` | Officer, Admin |
| Anomaly queue | `#/anomalies` | `GET /api/analytics/anomalies`, `…/investigate`, `PATCH …/anomalies/{id}` | Officer, Admin |
| Reports | `#/reports` | `POST /api/reports/export`, `GET /api/reports/{id}` | Admin |

Charts follow a validated colour palette in both light and dark mode, never rely on colour
alone, and every chart has a table view. Periods with no data show as gaps, never as zero.

Other components add their pages under `src/pages/` and a link in `NAV` in `App.tsx`.
