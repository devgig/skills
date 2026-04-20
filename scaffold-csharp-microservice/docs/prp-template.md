# PRP — <ServiceName>

> Fillable template. Copy this file to your new repo as `prp.md`, fill every
> section, then run `/scaffold-csharp-microservice` (or tell Claude: "scaffold
> a C# microservice from this PRP"). See `how-to-write-a-prp.md` for guidance
> on each section and `example-prp.md` for a worked example.

---

## 1. Service

- **Service name (PascalCase):** `<ServiceName>`
- **Kebab name:** `<service-name>`
- **Database name:** `<ServiceName>Db`
- **Target directory:** `/absolute/path/to/new-repo`
- **Consumer (Kafka)?** no  <!-- default off; change to yes only if you extend the skill -->

---

## 2. Entities

Repeat an `### Entity:` block for each entity in the database.

### Entity: <EntityName>

- **Collection name:** `<plural-lowercase>`

**Fields**

| Field name | C# type    | Nullable? | Default      | BSON name (if different) | Notes                    |
|------------|------------|-----------|--------------|--------------------------|--------------------------|
| Id         | string     | no        | empty string | _id                      | ObjectId, primary key    |
| <field>    | <type>     | yes/no    | <default>    |                          | <constraint>             |

**Indexes**

- `<field>` unique asc
- (or "None")

**Query methods** (on top of the inherited `BaseRepository<T>` API)

| Method signature                                           | Returns                    | Behavior                            |
|------------------------------------------------------------|----------------------------|-------------------------------------|
| `GetByEmailAsync(string email, CancellationToken ct)`      | `Task<<EntityName>?>`      | Find one by email, null if missing  |

**Validation notes** (free text — consumed when endpoints reference this entity)

- <rule>
- <rule>

---

## 3. Endpoints

Repeat an `### Endpoint:` block for each route.

### Endpoint: Create<Resource>

- **Method:** POST
- **Route:** `/api/<resource>`
- **Name:** `Create<Resource>`
- **Path params:** none
- **Query params:** none
- **Request body:**

  | Field      | Type   | Required? | Notes        |
  |------------|--------|-----------|--------------|
  | <field>    | <type> | yes/no    | <constraint> |

- **Response body (201):**

  | Field      | Type   | Notes              |
  |------------|--------|--------------------|
  | id         | string | new record id      |
  | <field>    | <type> |                    |

- **Status codes:** 201 Created, 400 ValidationProblem, 500
- **Validation:**
  - <rule>
- **Handler behavior:**
  1. <step>
  2. <step>

---

## 4. Business rules

Logic that spans multiple handlers or doesn't fit plain CRUD. For each rule,
state **what** and **where it lives** (which handler or domain service). If
there are none, write "None".

- <rule — where it lives>

---

## 5. Tests (optional)

Two ways to supply:

**Inline:** paste each test class below as a fenced code block under its own
`### Test:` heading — the skill extracts each block to `tests-inbox/` before
running.

### Test: <Feature>Tests.cs

```csharp
// paste your xUnit test class here
```

**By reference:** drop files into
`~/repos/skills/scaffold-csharp-microservice/tests-inbox/` and list names:

- `<Feature>Tests.cs`

If no tests yet, write "None — add later."
