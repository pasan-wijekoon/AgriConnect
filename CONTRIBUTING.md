# Contributing to AgriConnect

Conventions for the four-person team. These are the rules already in use on the repo — this document writes them down so nobody has to guess.

---

## Team & Component Ownership

| Member | Component | Branch |
|---|---|---|
| Student 1 | A — Produce Listings & Price Discovery | `feature/component/Produce_Listings_and_Price_Discovery` |
| Student 2 | B — Order & Collection-Centre Logistics | `feature/component/Order_and_Collection_Centre_Logistics` |
| Student 3 | C — Quality Grading & Inspection | `feature/component/Quality_Grading_and_Inspection` |
| Student 4 | D — Market Price Analytics & Reporting | `feature/component/Market_Price_Analytics_and_Reporting` |

Each member owns their component's entities, services, controllers, React pages, Flutter screens, and one Agentic AI agent. Work inside your own component's files by default; anything shared needs a heads-up to the team (see [Shared Files](#shared-files--touch-with-care)).

---

## Branching

```
main                              Stable. Protected. Demo-ready at all times.
└── development                   Integration branch. Components merge here first.
    └── feature/component/<Name>   One long-lived branch per member.
```

**Branch naming:** `feature/component/<Component_Name>` using `Title_Case_With_Underscores`.

For short-lived work off your component branch:

```
fix/<short-description>
chore/<short-description>
docs/<short-description>
```

Never commit directly to `main`.

---

## Commit Messages

This repo uses [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): short imperative description
```

### Types

| Type | Use for |
|---|---|
| `feat` | New functionality |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `test` | Adding or fixing tests |
| `refactor` | Restructuring with no behaviour change |
| `chore` | Build, CI, tooling, dependencies |

### Scopes

Use the area you touched: `analytics`, `listings`, `orders`, `inspection`, `reports`, `web`, `mobile`, `ai`, `ci`, `db`.

### Examples from this repo

```
feat(analytics): add Component D domain entities
feat(analytics): add DbContext and Component D migration
docs(component-d): add Member 4 development map
```

### Rules

- Imperative mood — "add", not "added" or "adds".
- No trailing full stop in the subject line.
- Keep the subject under ~72 characters.
- One logical change per commit. If the subject needs "and", it is probably two commits.

---

## Pull Requests

1. Push your component branch.
2. Open a PR into `development` (not `main`).
3. Title the PR with the same Conventional Commit format.
4. Describe **what** changed and **why**, and list anything that affects other components.
5. Get at least one review from a teammate — ideally whoever owns a component you depend on.

### PR checklist

- [ ] Builds locally (`dotnet build`, `npm run build`, `flutter build`)
- [ ] Tests pass
- [ ] No secrets, API keys, or real credentials committed
- [ ] Migrations added if entities changed
- [ ] `backend.http` updated if endpoints were added
- [ ] Teammates notified if a shared file was touched

Keep PRs small. One [commit slice](documentation/Member4_ComponentD_DevelopmentMap.md#5-commit-slices-one-pr-per-slice-keeps-review-sane) per PR is the target — a 40-file PR will not get a real review from anyone.

---

## Shared Files — Touch With Care

These are edited by everyone and are where merge conflicts actually happen. **Tell the team before changing them.**

| File | Why it is contested | How to work in it safely |
|---|---|---|
| `backend/src/config/AgriConnectDbContext.cs` | All four components register entities here | Add your `DbSet`s under your component's comment block; put Fluent API config in your own `ConfigureComponentX` method |
| `backend/Program.cs` | All service registrations | Add your registrations in one contiguous block, commented with your component |
| `backend/src/migrations/` | EF diffs against a shared snapshot | See [Migration Protocol](#migration-protocol) below |
| `web/src/App.tsx` | Shared routing | Add routes for your pages only |
| `mobile/lib/main.dart` | Shared app shell | Same — your screens only |
| `docker-compose.yml` | Shared services | Discuss before adding a service |

---

## Migration Protocol

EF Core generates migrations as a diff against a **shared model snapshot**. Two people generating migrations from different snapshots produces migrations that conflict and may not apply in order. This is the single most disruptive thing that can go wrong in this repo.

**Before creating any migration:**

```powershell
git pull origin development
```

```powershell
dotnet ef database update
```

Only then:

```powershell
dotnet ef migrations add YourMigrationName
```

**Rules:**

1. **Announce it.** Post in the team chat before you generate a migration — a 10-second message prevents an hour of untangling.
2. **Name it after your component.** `AddComponentDAnalyticsTables`, not `Update1`.
3. **Never edit a pushed migration.** Someone may have applied it. Add a new one.
4. **Merge migrations promptly.** A migration sitting unmerged on your branch for a week guarantees a conflict.

### Shared reference tables

`Crop`, `Region`, and `User` are needed by multiple components. **One person owns each** — agree who in week 1, and let them land it. Everyone else references the `Guid` and waits.

Component D currently maps `CropId`, `RegionId`, `ListingId`, and `RequestedBy` as indexed `Guid` columns **without FK constraints** so it can build standalone. A follow-up migration adds the real constraints once those tables exist.

---

## Architecture Rules (Non-Negotiable)

These come from the assignment specification and the design document. A PR that violates one gets rejected regardless of how well it works.

| Rule | Meaning |
|---|---|
| **Clients talk only to the API** | Flutter and React never touch PostgreSQL, the AI service, or any third-party API directly |
| **The AI service never writes to the database** | It returns a structured proposal; the ASP.NET Core API persists it |
| **Every AI proposal needs human approval** | An Officer must Approve / Reject / Request Revision before anything takes effect |
| **The API is the only write path** | Identity, authorization, validation, and persistence all live there |
| **React and Flutter serve different purposes** | React is back-office tooling; Flutter is field/transactional. Not two skins on the same features |

Full detail in [`documentation/AgriConnect_DFD.md`](documentation/AgriConnect_DFD.md) §2.5 and §2.6.

---

## Code Style

| Stack | Convention |
|---|---|
| C# | `PascalCase` for types/methods/properties, `camelCase` for locals. Nullable enabled — respect it |
| TypeScript | `PascalCase` components, `camelCase` functions/variables. `npm run lint` before pushing |
| Dart | `lowerCamelCase` members, `snake_case.dart` filenames |
| Python | PEP 8, `snake_case`, type hints on public functions |

**Comments:** explain *why*, not *what*. Well-named code already says what it does. Document non-obvious constraints, invariants, and decisions that would otherwise surprise a reader.

---

## Before You Push

```powershell
git pull origin development
```

Resolve conflicts locally, confirm it still builds, then push. Do not push a broken build to a shared branch — three other people are working off it.
