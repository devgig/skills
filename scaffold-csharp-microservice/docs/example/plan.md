# plan.md — UserService

Technical plan derived from [`spec.md`](./spec.md) and
[`../../constitution.md`](../../constitution.md). Everything on this page is
*how* — the *what* and *why* live in the spec.

## Architecture

Single ASP.NET Core process, stateless, reads/writes MongoDB, exposes HTTP
to an internal network. CQRS via MediatR inside the process; no external
message bus in this scaffold.

```
┌────────────────────────────────────────────┐
│  Service (ASP.NET Core minimal APIs)       │
│   ┌──────────────────────────────────────┐ │
│   │ Endpoints → Validators → ISender     │ │
│   └──────┬───────────────────────────────┘ │
│          │ MediatR                         │
│   ┌──────▼───────────────────────────────┐ │
│   │ Commands, Queries, Handlers          │ │
│   │   (Application project)              │ │
│   └──────┬───────────────────────────────┘ │
│          │ IUserRepository                 │
│   ┌──────▼───────────────────────────────┐ │
│   │ UserRepository : BaseRepository<User>│ │
│   │   (Domain / Infrastructure projects) │ │
│   └──────┬───────────────────────────────┘ │
│          ▼                                 │
└──────────┼─────────────────────────────────┘
           │ MongoDB driver
           ▼
       ┌────────┐
       │MongoDB │
       └────────┘
```

## Technology decisions

All decisions inherit from the [constitution](../../constitution.md). This
service takes no deviations.

| Concern | Choice |
|---|---|
| HTTP framework | ASP.NET Core 8 minimal APIs |
| Validation | FluentValidation (inline in endpoint lambdas) |
| CQRS | MediatR 13 |
| Persistence | MongoDB 3.4 via `MongoDB.Driver` |
| Repository | `BaseRepository<T>` (generic Mongo CRUD) |
| Logging | Serilog (Console + rolling File) |
| Tracing | OpenTelemetry → OTLP/gRPC to Tempo |
| Metrics | OpenTelemetry → Prometheus scrape at `/metrics` |
| Error shape | RFC 7807 ProblemDetails via `GlobalExceptionHandler` |
| Tests | xUnit + Moq + FluentAssertions (unit); EphemeralMongo (integration) |
| Container base | `mcr.microsoft.com/dotnet/aspnet:8.0` |

## File layout

```
UserService/
  UserService.sln
  catalog-info.yaml                   (generated from spec.md §Service)
  Dockerfile
  src/
    Service/
      Program.cs                      (MediatR, Mongo, validators, OTel, exception handler wiring)
      appsettings.json                (Serilog + Mongo + OTel config)
      Properties/launchSettings.json  (ports 5070 / 7273)
      Endpoints/UserEndpoints.cs      (7 routes — see contracts/users-api.yaml)
      Models/CreateUserRequest.cs, …  (records per endpoint)
      Validators/CreateUserRequestValidator.cs, …
      Middleware/GlobalExceptionHandler.cs
      Extensions/{Serilog,MongoDb,OpenTelemetry}Extensions.cs
    Application/
      Commands/CreateUserCommand.cs, UpdateUserCommand.cs, DeleteUserCommand.cs
      Queries/GetUserByIdQuery.cs, GetUserByEmailQuery.cs, GetAllUsersQuery.cs
      Handlers/*Handler.cs
      AssemblyMarker.cs
    Domain/
      Entities/User.cs
      Repositories/{IUserRepository,UserRepository}.cs
    Infrastructure/
      Common/BaseRepository.cs
      Interfaces/IRepository.cs
  tests/
    UnitTests/Application/Handlers/…
    IntegrationTests/UserRepositoryIntegrationTests.cs
```

## Cross-cutting concerns

- **Logging.** Serilog is configured in `Extensions/SerilogExtensions.cs`.
  Every log line carries `MachineName`, `ProcessId`, `ThreadId`,
  `Application=UserService`, plus an ambient `TraceId` when one exists.
- **Tracing.** `Extensions/OpenTelemetryExtensions.cs` registers AspNetCore,
  HTTP client, and MongoDB driver sources; traces export via OTLP/gRPC to
  the endpoint in `appsettings.json` (`OpenTelemetry:Tempo:Endpoint`).
  `ErrorAwareSampler` forces sampling on error spans.
- **Metrics.** Prometheus scrape endpoint at `/metrics`, plus AspNetCore,
  HTTP client, and .NET runtime meters.
- **Errors.** `Middleware/GlobalExceptionHandler.cs` maps exception types to
  RFC 7807 responses:
  - `ValidationException` → 400 with `errors` extension.
  - `ArgumentNullException` / `ArgumentException` → 400.
  - `KeyNotFoundException` → 404.
  - `InvalidOperationException` → 409 (used by B-1 email uniqueness).
  - Anything else → 500 with a generic message; the exception detail goes
    to the log, not the caller.

## Deployment notes

- **Container port:** 8080 (set in `Dockerfile` via `ASPNETCORE_HTTP_PORTS`).
- **Local dev port:** 5070 HTTP, 7273 HTTPS (from `launchSettings.json`).
- **Mongo connection:** `ConnectionStrings:MongoDB`. Default local is
  `mongodb://localhost:27017/UserServiceDb`. Override per environment.
- **OTLP endpoint:** `OpenTelemetry:Tempo:Endpoint`. Default
  `http://localhost:4317`.
- **Kubernetes:** manifests live in `kustomize/` (not scaffolded by this
  skill — see the `scaffold-go-microservice` pattern for the house Kustomize
  layout).
- **Backstage:** `catalog-info.yaml` is emitted alongside the code. Register
  the repo URL in Backstage after first push.

## Not in this plan

Anything not explicitly listed here is not implemented. In particular:
auth, rate limiting, idempotency keys, event publishing, and caching are
out of scope and must be added via follow-on specs.
