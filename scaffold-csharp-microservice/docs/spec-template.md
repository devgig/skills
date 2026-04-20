# spec.md — <ServiceName>

> **Monolithic spec template.** Use this when the service is small enough
> that one file is clearer than a decomposed `specs/<slug>/{spec,plan,
> data-model,contracts,tasks}` tree. For larger services, copy this file as
> `specs/001-initial-service/spec.md` and split the `Entities`,
> `API contracts`, and `Tasks` sections into separate files per the
> [how-to-write-a-spec guide](./how-to-write-a-spec.md).

---

## Service

Identity and Backstage catalog metadata.

- **Name (PascalCase):** `<ServiceName>`
- **Kebab name:** `<service-name>`
- **Database name:** `<ServiceName>Db`
- **Target directory:** `/absolute/path/to/new-repo`
- **Backstage system:** `<system-slug>`
- **Backstage owner (team):** `<team-slug>`
- **Lifecycle:** `experimental` | `production` | `deprecated`
- **Consumer (Kafka)?** no
- **Depends on:** `resource:mongodb` (+ any others)

## User stories

Who uses the service, what do they do, why does it matter? 3–5 short bullets
in the form *"As <role>, I want to <action>, so that <outcome>."*

## Scope

**In scope**

- <capability>
- <capability>

**Out of scope** (just as important as "in scope")

- <non-capability — say why if useful>

## Entities

Repeat a `### Entity:` block per entity. See
[`example/data-model.md`](./example/data-model.md) for a worked example.

### Entity: <EntityName>

- **Collection:** `<plural-lowercase>`

**Fields**

| Field name | C# type    | Nullable? | Default      | BSON name | Notes |
|------------|------------|-----------|--------------|-----------|-------|
| Id         | string     | no        | empty string | _id       | ObjectId, primary key |

**Indexes**

- `<field>` unique asc (or "None")

**Query methods** (on top of `BaseRepository<T>`)

| Method signature | Returns | Behavior |
|------------------|---------|----------|

**Validation notes**

- <rule>

## API contracts

Repeat an `### Endpoint:` block per route. See
[`example/contracts/users-api.yaml`](./example/contracts/users-api.yaml) for
a worked example.

### Endpoint: <Name>

- **Method / Route:** `VERB /api/<path>`
- **Name:** `<OperationId>`
- **Path params:** `<name> (<type>)`
- **Query params:** table `Name | Type | Required? | Default | Notes`
- **Request body:** table `Field | Type | Required? | Notes` (or "none")
- **Response body (STATUS):** table
- **Status codes:** `200, 400, 404, 500`
- **Validation:** rules
- **Handler behavior:** numbered steps

## Business rules

Cross-handler invariants. Each rule = one-sentence contract + "where it lives".

- <rule — where it lives in code>
- (or "None")

## Success criteria

Measurable statements of "done". Include at least one of:

- Functional: "all endpoints listed above return the documented status codes".
- Tests: "the tests in the Tests section pass under `dotnet test`".
- Observability: "every request produces a span and a log line enriched with
  `TraceId`".

## Tasks

Agent's ordered to-do. One action per line; agent ticks the box when done.

- [ ] **T001** Scaffold solution + `dotnet build` passes
- [ ] **T002** Generate entities
- [ ] **T003** Generate endpoints
- [ ] **T004** Implement business rules
- [ ] **T005** Run `dotnet test` — all provided tests pass
- [ ] **T006** Emit `catalog-info.yaml` from service-identity fields

## Tests

Two ways to supply:

**Inline:** paste each test class under `### Test: <FileName>Tests.cs` as a
fenced ```csharp code block. The skill extracts each block to the right
test project before running.

**By reference:** drop files into the spec's `tests/` folder or
`~/repos/skills/scaffold-csharp-microservice/tests-inbox/`, then list them:

- `<Feature>Tests.cs`

If none yet, write "None — run after scaffold and add later."
