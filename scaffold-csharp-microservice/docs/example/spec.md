# spec.md — UserService

## Service

| Field | Value |
|---|---|
| Name (PascalCase) | `UserService` |
| Kebab name | `user-service` |
| Database name | `UserServiceDb` |
| Target directory | `/Users/<you>/repos/UserService` |
| Backstage system | `user-management` |
| Backstage owner (team) | `dream-team` |
| Lifecycle | `production` |
| Consumer (Kafka)? | no *(the real repo has one; omitted here)* |
| Depends on | `resource:mongodb` |

## User stories

- As a **consuming service**, I want to create, read, update, and delete
  user records so that I can manage identity in my own workflows.
- As a **platform operator**, I want every mutation to emit a structured
  log line and a trace span so that I can debug production issues from
  Grafana.
- As a **developer onboarding to the org**, I want the UserService to look
  like every other C# microservice so that I don't re-learn the layout per
  project.

## Scope

**In scope**

- CRUD over the `User` entity: `Id`, `FirstName`, `LastName`, `Email`,
  `CreatedAt`, `UpdatedAt`.
- Lookup by id (Guid) and by email.
- Paginated list.
- Email uniqueness on create.
- Structured logging + OpenTelemetry traces + Prometheus metrics.
- RFC 7807 problem-details error responses.

**Out of scope**

- **Authentication / authorization.** Callers are assumed to be inside a
  trusted network. Add post-scaffold if that changes.
- **Soft delete.** `DELETE` removes the document.
- **Event publishing.** The real repo has a Kafka consumer; event emission
  is not modeled here.
- **Uniqueness on update.** Matches current UserService behavior (a known
  gap in the real service).

## Business rules

- **B-1 · Email uniqueness on create.** No two users may share an email
  address. *Where it lives:* `CreateUserCommandHandler`, via
  `IUserRepository.GetByEmailAsync`. Violation throws
  `InvalidOperationException` → 409 via `GlobalExceptionHandler`.
- **B-2 · Timestamp stamping.** `CreatedAt` is set on insert; `UpdatedAt`
  is set on every replace. *Where it lives:* `UserRepository.SaveAsync`,
  branching on whether `Id` is empty.
- **B-3 · Id projection.** The repository stores `Id` as a MongoDB
  `ObjectId`, but every endpoint exposes it as `Guid` to the caller.
  *Where it lives:* Response DTO projections (`Guid.Parse(savedUser.Id)`).

## Success criteria

- All endpoints in [`contracts/users-api.yaml`](./contracts/users-api.yaml)
  return the documented status codes.
- Every test in [`tests/`](./tests/) passes under `dotnet test`.
- A `POST /api/user` with a duplicate email returns `409 Conflict` with an
  RFC 7807 body.
- Every request produces a log line and an OpenTelemetry span; the trace id
  is exposed in problem-details responses under `extensions.traceId`.

## Related artifacts

- [`plan.md`](./plan.md) — the how.
- [`data-model.md`](./data-model.md) — the User entity.
- [`contracts/users-api.yaml`](./contracts/users-api.yaml) — API contract.
- [`tasks.md`](./tasks.md) — agent task list.
- [`tests/`](./tests/) — acceptance tests.
- [`catalog-info.yaml`](./catalog-info.yaml) — Backstage descriptor generated
  from the service-identity fields above.
