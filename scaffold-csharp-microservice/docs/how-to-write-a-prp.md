# How to write a Product Requirements Prompt (PRP)

A **PRP** is a single markdown file that tells the `scaffold-csharp-microservice`
skill everything it needs to generate your service: identity, database model,
HTTP contracts, business rules, and optional acceptance tests. One file — no
scattered config.

Think of it as the spec you'd hand to a teammate on day one: "here's the
service we're building, here's the data, here's the API surface, here are the
rules that don't fit neatly into a CRUD handler, and here are the tests that
prove it works."

This guide explains *what* to put in each section and *why*. The fillable
template is at [`prp-template.md`](./prp-template.md). A complete worked
example — a PRP that reverses the existing UserService back into a spec — is
at [`example-prp.md`](./example-prp.md).

---

## The five sections

### 1. Service

Identity + infrastructure defaults. One-time answers.

- **Service name** (PascalCase, e.g. `OrderService`) — drives the solution
  name, OpenTelemetry ServiceName, Serilog Application property.
- **Kebab name** — `order-service`. Used in Docker labels, image repo.
- **Database name** — MongoDB database, usually `<Service>Db`.
- **Target directory** — absolute path to write the generated code into.

The skill asks for these even if you list them in the PRP — listing them makes
the PRP self-contained (re-runnable).

### 2. Entities

What the database holds. One `### Entity: <Name>` block per entity.

- **Collection name** — MongoDB collection, plural lower-case.
- **Fields** — table with `Field name | C# type | Nullable? | Default |
  BSON name (if different) | Notes`. Include every persisted field; the skill
  will not invent fields you don't list, not even `CreatedAt`.
- **Indexes** — one per line: `field1 [+ field2]  unique|non-unique  asc|desc`.
- **Query methods** — any query on top of the inherited `BaseRepository<T>`
  methods (`GetByIdAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync`,
  `DeleteAsync`, `CountAsync`, `ExistsAsync`). Table format:
  `Method signature | Returns | Behavior`.
- **Validation notes** — plain-text rules that will be turned into
  FluentValidation rules when endpoints reference this entity.

Why it's structured this way: the skill converts each row of the fields table
into a property on the C# class, each index line into a
`_collection.Indexes.CreateOne(...)` call, and each query method into a
signature on both the interface and the implementation. If a row is missing,
the generated code is missing it too.

### 3. Endpoints

The API contract. One `### Endpoint: <Name>` block per route.

- **Method** — `GET`/`POST`/`PUT`/`PATCH`/`DELETE`.
- **Route** — the exact path, including template params (`/api/order/{id:guid}`).
- **Name** — used in `.WithName(...)` for Swagger operation IDs.
- **Path params / Query params** — list name, type, required, default, notes.
- **Request body** — table: `Field | Type | Required? | Notes`. `none` if no body.
- **Response body (STATUS)** — repeat per status code that has a body.
- **Status codes** — every code that should appear in `.Produces(...)`.
- **Validation** — what FluentValidation should enforce (string lengths,
  formats, ranges). The skill generates a validator matching these rules.
- **Handler behavior** — numbered steps describing what the handler does.
  Be explicit: "1. Load by id. 2. If null, throw `KeyNotFoundException`. 3.
  Apply fields. 4. Save." The skill implements these steps literally; ambiguous
  or missing steps yield ambiguous or missing code.

### 4. Business rules

Logic that doesn't fit in one handler — cross-entity invariants, derived
values, calls to other services, domain events. Write each as a short
paragraph plus a note on **where it lives** (which handler or domain service).

Examples:
- "Email addresses are globally unique. When creating or updating a user,
  reject with `InvalidOperationException` if another user already has the same
  email. Lives in `CreateUserCommandHandler` and `UpdateUserCommandHandler`."
- "Orders over $10,000 require a manager-approval flag. Reject with
  `ValidationException` if missing. Lives in `CreateOrderCommandHandler`."

If you don't have any business rules beyond plain CRUD, write "None" — don't
delete the section. The skill checks it exists.

### 5. Tests (optional)

Acceptance criteria the skill **runs** before declaring success. Two ways to
supply tests:

**Inline** — put each test class inside a fenced ```` ```csharp ```` block
under `### Test: <FileName>Tests.cs`. The skill extracts each block to
`tests-inbox/` automatically.

**By reference** — drop files into
`~/repos/skills/scaffold-csharp-microservice/tests-inbox/` (or any folder you
pass to the skill) and list the filenames here.

The skill routes each test to Unit or Integration based on its `using`
statements, runs `dotnet test`, and reports results. It will **not** modify
your tests to make them pass.

If you don't have tests yet, leave the section as "None — run after scaffold
and add later."

---

## Rules of thumb when writing a PRP

1. **Be exact about types and nullability.** `string` vs `string?` matters.
2. **Don't describe implementation.** Say *what* the handler does, not *how*
   (no LINQ, no method names — those are output, not input).
3. **One PRP = one service.** Don't bundle multiple services. Run the skill
   twice.
4. **If a field isn't in the PRP, it isn't in the code.** The skill refuses to
   invent. This is a feature — it stops silent drift between spec and code.
5. **Keep the PRP in the generated repo.** Name it `prp.md` at the repo root.
   Re-running the skill after editing the PRP keeps the spec and code in sync.

---

## When to re-run the skill

- You changed the PRP and want to regenerate. The skill overwrites only files
  it owns (entities, repositories, DTOs, validators, commands, queries,
  handlers, endpoint registration). It leaves business-rule edits, tests, and
  anything you've added by hand alone **unless** you explicitly ask it to
  re-sync.
- You added tests to `tests-inbox/` and want them run.
- You want to verify the scaffolded service still builds and tests pass after
  your own edits.

## What the skill won't touch

- `Program.cs` outside of the two marker comments.
- Anything under `src/Service/Middleware/` (other than generating one).
- Your `README.md`, `Dockerfile`, `appsettings.json` after initial scaffold.
- Tests in `tests/UnitTests/` or `tests/IntegrationTests/` that you've edited.

If you want full regeneration, delete the generated files and re-run. The PRP
is the source of truth — the code is a projection.
