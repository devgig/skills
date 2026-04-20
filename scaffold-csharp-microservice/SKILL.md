---
name: scaffold-csharp-microservice
description: Scaffold a new .NET 8 (C#) microservice matching the UserService reference architecture — minimal APIs, MediatR CQRS, MongoDB with BaseRepository<T>, FluentValidation, Serilog, OpenTelemetry (Prometheus + OTLP), RFC 7807 GlobalExceptionHandler, xUnit unit + integration tests. Takes a product requirements prompt (PRP) that declares the service name, database entities, endpoint contracts, and any business logic, and produces a compiling, runnable service. Use when the user asks to create a new C# microservice, scaffold a .NET service, bootstrap a service from a PRP, or generate a service "like UserService".
---

# Scaffold a C# Microservice

Generate the files for a new .NET 8 HTTP microservice matching the UserService house layout: a solution with **Service / Application / Domain / Infrastructure** projects, two test projects (**UnitTests / IntegrationTests**), MongoDB via `BaseRepository<T>`, MediatR-based CQRS, FluentValidation, Serilog, OpenTelemetry, and Kubernetes-ready Dockerfile.

The templates in `templates/` are the **scaffolding** — the generic, non-domain-specific files that every service starts with. The user's **PRP** (product requirements prompt — see `docs/`) declares the domain-specific pieces: entities, endpoints, validation rules, and any specific business logic. Combining the two produces a compiling service.

## Inputs to collect

Ask the user for all of these **in one batched question** — do not ask one at a time:

| Placeholder | Meaning | Example |
|---|---|---|
| `{{SERVICE_NAME}}` | PascalCase service name; used for solution file, assembly labels, OpenTelemetry ServiceName, Serilog Application property | `OrderService` |
| `{{SERVICE_NAME_KEBAB}}` | kebab-case; used in Docker labels, image repo name, Kubernetes app label | `order-service` |
| `{{DATABASE_NAME}}` | MongoDB database name | `OrderServiceDb` |
| `{{TARGET_DIR}}` | Absolute path where the new repo should be written | `/Users/me/repos/OrderService` |
| `{{PRP_PATH}}` | Absolute path to the filled-in PRP file | `/Users/me/repos/OrderService/prp.md` |

Derived tokens (compute — do not re-ask):

- If the user doesn't provide `{{SERVICE_NAME_KEBAB}}`, derive it from `{{SERVICE_NAME}}` (split on camel-case, lowercase, hyphenate).
- If the user doesn't provide `{{DATABASE_NAME}}`, default to `{{SERVICE_NAME}}Db`.

## Workflow

Run these phases **in order**. Do not skip ahead. After each phase, run `dotnet build` at the solution root and report the result — fail loudly if the build breaks and fix before moving on.

### Phase 1 — Read and validate the PRP

1. Read the file at `{{PRP_PATH}}`.
2. Verify it has all required sections (see `docs/prp-template.md`): **Service**, **Entities**, **Endpoints**, **Business rules**, optional **Tests**.
3. If anything required is missing, **stop and tell the user exactly which sections need filling in**. Do not invent fields, routes, or types.
4. Confirm the plan back to the user: list the entities (name + field count), endpoints (verb + route), and any business rules. Wait for confirmation before writing files.

### Phase 2 — Scaffold the base files from `templates/`

1. Verify `{{TARGET_DIR}}` exists; create it if not. If it contains existing files at paths this skill writes, stop and ask before overwriting.
2. For every file under `templates/`, create the same relative path under `{{TARGET_DIR}}` and write the content. File naming rule:
   - If the source path ends in `.tmpl`, strip the suffix and substitute every `{{PLACEHOLDER}}` token.
   - Otherwise copy the file verbatim.
3. Create the solution and add projects:
   ```bash
   cd {{TARGET_DIR}}
   dotnet new sln -n {{SERVICE_NAME}}
   dotnet sln add src/Service/Service.csproj src/Application/Application.csproj src/Domain/Domain.csproj src/Infrastructure/Infrastructure.csproj tests/UnitTests/UnitTests.csproj tests/IntegrationTests/IntegrationTests.csproj
   ```
4. Run `dotnet build`. Must return 0 errors before proceeding.

### Phase 3 — Generate domain entities + repositories from the PRP

For each entity in the PRP's **Entities** section:

1. `src/Domain/Entities/<Name>.cs` — POCO with BSON attributes. Use `[BsonId][BsonRepresentation(BsonType.ObjectId)] public string Id` unless the PRP specifies otherwise. Nullability, types, and defaults come **only** from the spec — never invent `CreatedAt`/`UpdatedAt` unless declared.
2. `src/Domain/Repositories/I<Name>Repository.cs` — interface extending `IRepository<<Name>>` with every **Query method** listed in the PRP.
3. `src/Domain/Repositories/<Name>Repository.cs` — inherits `BaseRepository<<Name>>`, implements each query method with `_collection.Find(...)` using the declared field names. Initialize indexes from the PRP's **Indexes** section in the constructor.
4. Register in `Program.cs`: insert `builder.Services.AddScoped<I<Name>Repository, <Name>Repository>();` at the `// {{REPOSITORY_REGISTRATIONS}}` marker.
5. Run `dotnet build`. Fix errors before moving on.

### Phase 4 — Generate endpoints + DTOs + handlers + validators from the PRP

For each `## Endpoint:` block in the PRP:

1. **Request DTO** — `src/Service/Models/<Verb><Resource>Request.cs` as a `record` (skip if no request body).
2. **Response DTO** — `src/Service/Models/<Verb><Resource>Response.cs` as a `record`.
3. **Validator** — `src/Service/Validators/<Verb><Resource>RequestValidator.cs` using FluentValidation, rules derived from the **Validation** section. Create a trivial validator even if the spec is short; do not skip.
4. **MediatR artifact**:
   - State-changing (POST/PUT/PATCH/DELETE) → `src/Application/Commands/<Verb><Resource>Command.cs`.
   - Read-only (GET) → `src/Application/Queries/<Verb><Resource>Query.cs`.
5. **Handler** — `src/Application/Handlers/<Verb><Resource>{Command|Query}Handler.cs`, sealed class implementing `IRequestHandler<TRequest, TResponse>`. Inject `I<Resource>Repository`. Implement the numbered steps from **Handler behavior** literally; do not add steps the spec doesn't list.
6. **Endpoint registration** — append to (or create) `src/Service/Endpoints/<Resource>Endpoints.cs` with a `Map<Resource>Endpoints` extension method. Use the pattern from the UserService reference — inline validator call raising `ValidationException`, then `ISender.Send(...)`, then `Results.Created` / `Ok` / `NoContent` per the spec. Add the `.WithName(...)`, `.Produces<T>(...)`, `.ProducesValidationProblem()`, and a `.Produces(...)` call for every status code listed in the spec.
7. Register in `Program.cs`: insert `app.Map<Resource>Endpoints();` at the `// {{ENDPOINT_REGISTRATIONS}}` marker (one call per resource, regardless of how many endpoints the resource has).
8. Run `dotnet build`. Fix errors before moving on.

### Phase 5 — Implement any business rules from the PRP

The PRP's **Business rules** section contains logic that doesn't fit cleanly into an individual handler — cross-entity invariants, calculated fields, external service calls, etc.

1. For each rule, identify which handler or domain service it belongs in.
2. Add it **only** to the code it affects — do not introduce new abstractions just to house the rule.
3. If the rule needs a new repository method, add it to the entity spec section of the PRP *conceptually* (telling the user), then add it to both `I<Name>Repository` and `<Name>Repository`.
4. Run `dotnet build`.

### Phase 6 — Run user-provided tests (acceptance gate)

1. Look for tests listed in the PRP's **Tests** section and/or files dropped into `tests-inbox/` of this skill folder.
2. For each test file, decide its target project:
   - Uses `Moq`, mocks a repository, tests a handler → `tests/UnitTests/`
   - Hits real MongoDB (or `EphemeralMongo`) or real HTTP endpoints via `WebApplicationFactory` → `tests/IntegrationTests/`
3. **Copy** (do not move) into the appropriate subfolder, mirroring the namespace/folder of the code under test (e.g. `CreateUserCommandHandlerTests.cs` → `tests/UnitTests/Application/Handlers/`).
4. Run `dotnet test --logger "console;verbosity=normal"` from the solution root.
5. Report exactly:
   ```
   Test results for {{SERVICE_NAME}}
   ---------------------------------
   Passed: N
   Failed: N
   Skipped: N

   Failures:
     - <fully-qualified test name>
         <first line of assertion message>
   ```
6. **Never modify a user-supplied test** to make it pass. Tests are the acceptance contract. If a test fails, propose a fix to the production code and ask the user before applying it.
7. If MongoDB is unavailable for integration tests, say so plainly — do not claim tests passed.

### Phase 7 — Report

Print a short tree of what was written, the endpoints table (verb, route, handler), the entities table (name, field count, indexes), and whether `dotnet test` fully passed.

## Substitution rules

- Substitute `{{PLACEHOLDER}}` tokens **literally** — do not rename identifiers beyond placeholders.
- Files named `*.tmpl` in `templates/` are renamed to drop the `.tmpl` suffix during scaffolding.
- Placeholders appear in file contents only; directory names are already generic.
- `Program.cs.tmpl` has two special marker comments (`{{REPOSITORY_REGISTRATIONS}}`, `{{ENDPOINT_REGISTRATIONS}}`) that Phases 3 and 4 insert into.

## Style rules (match the reference)

- DTOs are `record` types.
- Handlers are `sealed` classes implementing `IRequestHandler<,>`.
- Validators live under `Service/Validators/`, not in Application.
- Repositories inherit `BaseRepository<T>`; do not add new generic helpers to Infrastructure unless the PRP explicitly requires them.
- No business logic in endpoint lambdas — validate, send via MediatR, return `Results.*`.
- Entity `Id` is a `string` with `[BsonId][BsonRepresentation(BsonType.ObjectId)]` unless the PRP says otherwise.

## What this skill does NOT do

- **Auth.** No JWT, API key, or OAuth — the UserService reference doesn't have one either. Add after scaffolding if needed.
- **Kustomize / Kubernetes manifests.** Covered separately (see `scaffold-go-microservice` for the house Kustomize pattern; the user can copy the `kustomize/` tree across).
- **Kafka consumer.** The UserService has a `Consumer` project; this skill omits it by default to keep scaffolds lean. If the PRP lists "consumer: yes", prompt the user to add it manually or extend this skill.
- **Azure DevOps pipelines.** Out of scope.
- **Invent fields, endpoints, or business rules** the PRP doesn't declare — the PRP is the contract. When in doubt, stop and ask.

## Reference

The UserService repo at `~/repos/UserService` is the authoritative reference. `docs/example-prp.md` is a reversed-engineered PRP that, fed back through this skill, would reproduce that service. Read it alongside the UserService source to see how PRP sections translate into code.
