---
name: scaffold-csharp-microservice
description: Scaffold a .NET 8 (C#) microservice from a Spec-Driven Development (SDD) spec — the industry pattern popularized by GitHub Spec Kit. Reads `constitution.md` + `specs/<slug>/{spec.md, plan.md, data-model.md, contracts/, tasks.md, tests/}` and produces a compiling, runnable service matching the UserService reference architecture: minimal APIs, MediatR CQRS, MongoDB with `BaseRepository<T>`, FluentValidation, Serilog, OpenTelemetry (Prometheus + OTLP), RFC 7807 GlobalExceptionHandler, xUnit unit + integration tests, and a Backstage `catalog-info.yaml`. Use when the user asks to scaffold or regenerate a C# microservice from a spec, or create a new service "like UserService".
---

# Scaffold a C# Microservice (Spec-Driven)

You implement a service from a **spec** written in the GitHub Spec Kit
style. The vocabulary matters — this is the industry norm:

- **`constitution.md`** — project/platform-wide non-negotiables (e.g.
  "every service uses MediatR + BaseRepository + xUnit"). Lives at the
  skill root (`./constitution.md`) and also optionally at the target
  repo root (per-org override).
- **`spec.md`** — what & why.
- **`plan.md`** — how (architecture, tech choices).
- **`data-model.md`** — entities.
- **`contracts/<resource>.yaml`** — API contracts.
- **`tasks.md`** — ordered executable task list the agent chews through.
- **`tests/`** — acceptance tests (xUnit .cs files).
- **`catalog-info.yaml`** — Backstage descriptor, generated from `spec.md`.

The template files under `templates/` are the **scaffolding** — the
generic, non-domain-specific code every service starts with. Combine the
templates with the spec to produce a runnable service.

---

## Inputs to collect

Ask the user in **one batched question** (never one at a time). Most
answers live in `spec.md` §Service; the two you still need from the user
are the target directory and the spec path.

| Input | Source | Example |
|---|---|---|
| `{{TARGET_DIR}}` | user | `/Users/me/repos/OrderService` |
| `{{SPEC_DIR}}` | user | `{{TARGET_DIR}}/specs/001-initial-service/` (or a single `spec.md` path) |
| `{{SERVICE_NAME}}` | `spec.md` §Service | `OrderService` |
| `{{SERVICE_NAME_KEBAB}}` | `spec.md` §Service | `order-service` |
| `{{DATABASE_NAME}}` | `spec.md` §Service | `OrderServiceDb` |
| `{{SERVICE_DESCRIPTION}}` | `spec.md` summary | `Order processing microservice` |
| `{{OWNER}}` | `spec.md` §Service | `dream-team` |
| `{{SYSTEM}}` | `spec.md` §Service | `order-management` |
| `{{LIFECYCLE}}` | `spec.md` §Service | `experimental` / `production` |
| `{{RESOURCE_SLUG}}` | primary entity, lowercase | `orders` |

If `{{SPEC_DIR}}` is a single file `spec.md`, treat the spec as monolithic —
sections inside it play the roles of `plan.md`, `data-model.md`, and
`contracts/`. See `docs/spec-template.md` for the monolithic form.

---

## Workflow

Run these phases **in order**. After every phase, `dotnet build` must
return 0 errors before moving on. After Phase F, `dotnet test` must return
0 failures. If a gate fails, stop and report — do not skip ahead.

### Phase 0 — Constitution check

1. Read `./constitution.md` (this skill's root).
2. Read `{{TARGET_DIR}}/constitution.md` if present (per-repo override).
3. Merge: per-repo overrides win over skill-level. Report to the user which
   principles are in force.

### Phase 1 — Read & validate the spec

1. Read every file under `{{SPEC_DIR}}`: `spec.md`, `plan.md`,
   `data-model.md`, `contracts/*.yaml`, `tasks.md`, `tests/*.cs`.
2. Verify required sections exist. Missing sections = stop, tell the user
   exactly what's missing. Do not invent fields, endpoints, types, routes,
   validation rules, or business rules.
3. Confirm the plan back to the user: entities (name + field count),
   endpoints (verb + route), and business rules. Wait for confirmation
   before writing files.

### Phase 2 — Scaffold base from `templates/`

1. Verify `{{TARGET_DIR}}` exists; create it if not. If it contains files
   at paths this skill writes, stop and ask before overwriting.
2. For every file under `templates/`, create the same relative path under
   `{{TARGET_DIR}}` and write the content:
   - If the source ends in `.tmpl`, strip the suffix and substitute every
     `{{PLACEHOLDER}}`.
   - Otherwise copy verbatim.
3. Create the solution:
   ```bash
   cd {{TARGET_DIR}}
   dotnet new sln -n {{SERVICE_NAME}}
   dotnet sln add src/Service/Service.csproj src/Application/Application.csproj src/Domain/Domain.csproj src/Infrastructure/Infrastructure.csproj tests/UnitTests/UnitTests.csproj tests/IntegrationTests/IntegrationTests.csproj
   ```
4. **Gate:** `dotnet build`.

### Phase 3 — Entities from `data-model.md`

For each `## Entity: <Name>` block:

1. `src/Domain/Entities/<Name>.cs` — POCO with BSON attributes per the
   entity's **Fields** table. Nullability, types, defaults come **only**
   from the spec.
2. `src/Domain/Repositories/I<Name>Repository.cs` — interface extending
   `IRepository<<Name>>` with every row in **Query methods**.
3. `src/Domain/Repositories/<Name>Repository.cs` — inherits
   `BaseRepository<<Name>>`, implements each query method with
   `_collection.Find(...)`. Create indexes listed in **Indexes** in the
   constructor.
4. Insert `builder.Services.AddScoped<I<Name>Repository, <Name>Repository>();`
   at the `// {{REPOSITORY_REGISTRATIONS}}` marker in `Program.cs`.
5. **Gate:** `dotnet build`.

### Phase 4 — Endpoints from `contracts/<resource>.yaml`

For each path/operation in the contract:

1. **Request DTO** — `src/Service/Models/<OperationId>Request.cs` (record).
   Skip if no body.
2. **Response DTO** — `src/Service/Models/<OperationId>Response.cs` (record),
   built from the response schema per status code.
3. **Validator** — `src/Service/Validators/<OperationId>RequestValidator.cs`
   using FluentValidation, rules from the operation's `x-validation` block.
   Trivial validators are fine; missing validators are not.
4. **MediatR artifact:**
   - POST/PUT/PATCH/DELETE → `src/Application/Commands/<OperationId>Command.cs`.
   - GET → `src/Application/Queries/<OperationId>Query.cs`.
5. **Handler** —
   `src/Application/Handlers/<OperationId>{Command|Query}Handler.cs`,
   `sealed class : IRequestHandler<,>`. Inject the relevant repository.
   Implement the numbered steps in the operation's `x-handler` block
   **literally**.
6. **Endpoint registration** — append to
   `src/Service/Endpoints/<Resource>Endpoints.cs` with a
   `Map<Resource>Endpoints` extension method. Use the UserService pattern:
   inline `validator.ValidateAsync` throwing on failure, then `ISender.Send`,
   then `Results.*`. Add `.WithName`, `.Produces<T>`, `.ProducesValidationProblem`,
   and a `.Produces(...)` for every status code in the operation.
7. Insert `app.Map<Resource>Endpoints();` at the
   `// {{ENDPOINT_REGISTRATIONS}}` marker in `Program.cs` — one call per
   resource.
8. **Gate:** `dotnet build`.

### Phase 5 — Business rules from `spec.md`

The `## Business rules` section lists cross-handler invariants with an
explicit "where it lives" note.

1. For each rule, locate the named handler or domain service.
2. Add the rule's logic there **only** — do not introduce new abstractions.
3. If a rule needs a new repository method, amend `data-model.md` (tell the
   user), then add to the interface + implementation.
4. **Gate:** `dotnet build`.

### Phase 6 — Backstage descriptor

1. Generate `{{TARGET_DIR}}/catalog-info.yaml` from
   `templates/catalog-info.yaml.tmpl`, populating identity fields from
   `spec.md` §Service.
2. If the spec declares multiple API contracts or additional resources,
   extend the catalog record accordingly (see
   `docs/example/catalog-info.yaml` for the shape with an API, a Mongo
   Resource, and a System).

### Phase 7 — Run tests (acceptance gate)

1. Collect tests from: `{{SPEC_DIR}}/tests/`, the skill's `tests-inbox/`,
   and any inline ```csharp blocks under `### Test:` headings in `spec.md`.
2. Route each file to the right project:
   - Uses `Moq`, mocks a repository → `tests/UnitTests/`.
   - Uses `EphemeralMongo` or `WebApplicationFactory` → `tests/IntegrationTests/`.
3. Copy (don't move) into the appropriate subfolder, mirroring the
   namespace of the code under test.
4. Run `dotnet test --logger "console;verbosity=normal"` from the solution
   root.
5. Report exactly:
   ```
   Test results for {{SERVICE_NAME}}
   Passed: N   Failed: N   Skipped: N

   Failures:
     - <fully-qualified test name>
         <first assertion line>
   ```
6. **Never modify a user-supplied test** to make it pass. Tests are the
   contract. If a test fails, propose a fix to the production code and ask
   the user before applying.

### Phase 8 — Report

Tick off each task in `tasks.md`. Print:

- Tree of files written.
- Entities summary (name, fields count, indexes).
- Endpoints table (verb, route, handler).
- `catalog-info.yaml` location.
- `dotnet test` summary.

---

## Substitution rules

- Tokens are `{{PLACEHOLDER}}`, substituted literally. Do not rename
  identifiers beyond placeholders.
- `*.tmpl` files drop the suffix during scaffolding.
- `Program.cs.tmpl` has two marker comments
  (`{{REPOSITORY_REGISTRATIONS}}`, `{{ENDPOINT_REGISTRATIONS}}`) that
  Phases 3 and 4 insert into. Leave everything else in `Program.cs`
  untouched on re-runs.

## Style rules (match the reference)

- DTOs are `record` types.
- Handlers are `sealed class : IRequestHandler<,>`.
- Validators live under `src/Service/Validators/`, not in Application.
- Repositories inherit `BaseRepository<T>`; direct `IMongoCollection<T>`
  use outside the repository is prohibited.
- Entity `Id` is `string` with
  `[BsonId][BsonRepresentation(BsonType.ObjectId)]` unless the spec
  specifies otherwise.
- No business logic in endpoint lambdas — validate, send via MediatR,
  return `Results.*`.

## What this skill does NOT do

- **Invent** anything the spec doesn't declare. The spec is the contract.
  When in doubt, stop and ask.
- **Auth / authorization.** Out of scope. Add after scaffolding.
- **Kafka consumer.** Out of scope; the UserService reference has one
  separately. Scope a follow-on spec to add it.
- **Kubernetes manifests.** See the `scaffold-go-microservice` skill for
  the house Kustomize layout; copy the tree across after scaffolding.
- **Modify user-supplied tests.** Tests are the acceptance contract.

---

## Reference

- `./constitution.md` — the platform-level rules this skill enforces.
- `./docs/example/` — a fully-worked spec that reverse-engineers the
  existing UserService (`~/repos/UserService`). Open each file side-by-side
  with the real UserService source to see how spec sections project into
  code.
- `./docs/how-to-write-a-spec.md` — guide for authors.
- `./docs/spec-template.md` — blank monolithic starter when the decomposed
  layout feels heavy.
- [GitHub Spec Kit](https://github.com/github/spec-kit) — canonical
  vocabulary and directory layout this skill follows.
