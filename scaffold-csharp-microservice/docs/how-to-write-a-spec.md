# How to write a spec for `scaffold-csharp-microservice`

This skill follows **Spec-Driven Development (SDD)** — the industry pattern
popularized by [GitHub Spec Kit](https://github.com/github/spec-kit),
[AWS Kiro](https://kiro.dev/), and
[Thoughtworks' guidance on spec-driven development with AI](https://www.thoughtworks.com/insights/blog/generative-ai/spec-driven-development).
A spec is a **contract**: it tells the agent what to build, what not to
build, and how to verify the work.

For any non-trivial service, don't cram everything into one file. Decompose
as Spec Kit does:

```
<your-service-repo>/
  constitution.md                 (optional — inherits from ~/repos/skills/.../constitution.md)
  specs/
    <###>-<feature-slug>/
      spec.md                     what & why
      plan.md                     how (architecture, tech choices)
      data-model.md               entities (fields, indexes, query methods)
      contracts/                  API contracts (OpenAPI YAML or markdown tables)
        <resource>.yaml
      tasks.md                    ordered, executable task list for the agent
      tests/                      acceptance tests (xUnit .cs files)
        <Feature>Tests.cs
```

A worked, fully-filled-out example reverse-engineered from the existing
UserService lives at [`example/`](./example/). Read that alongside this
guide — each file there is a teaching artifact.

---

## The five artifacts

### 1. `spec.md` — what & why

The intent, audience, user stories, and scope. Kept short and
non-technical. *No implementation details.* Sections:

- **Service identity** — name, owner, Backstage system, lifecycle.
- **User stories** — who uses the service and what they accomplish.
- **Scope** — what's in, what's out (say both; the "out" list is the most
  valuable part of any spec).
- **Business rules** — cross-cutting invariants. Each rule is a
  one-sentence contract + a note on where it lives in code.
- **Success criteria** — measurable statements of "done" (traffic, latency,
  error budget, test pass rate).

### 2. `plan.md` — how

Technical plan derived from `spec.md` + the `constitution.md`. Describes
architecture choices, the solution layout, the dependencies, and any
deviations from the constitution (and why).

- **Architecture** — one paragraph + a simple diagram if it helps.
- **Technology decisions** — MediatR, MongoDB, OpenTelemetry, etc. Link back
  to the constitution; list only deviations here.
- **File layout** — which artifacts the scaffold produces.
- **Cross-cutting concerns** — logging, tracing, error handling.
- **Deployment notes** — Docker image, Kubernetes manifests, any external
  config.

### 3. `data-model.md` — entities

One `## Entity: <Name>` block per domain entity. Structure:

- **Collection name** (MongoDB).
- **Fields** table — `Field name | C# type | Nullable? | Default | BSON name | Notes`.
- **Indexes** — one per line.
- **Query methods** — custom methods beyond the inherited `BaseRepository<T>`
  API. Table: `Method | Returns | Behavior`.
- **State transitions** — if the entity has a lifecycle (Draft → Approved →
  Archived), draw the transitions.
- **Validation notes** — consumed by endpoints that reference the entity.

The skill refuses to invent fields or methods not in this file. That is a
feature — it stops silent drift between spec and code.

### 4. `contracts/<resource>.yaml` — API contracts

One file per resource. Format: an OpenAPI-flavored YAML block or a markdown
table (either is fine; pick one and stick to it). Include, per endpoint:

- HTTP method and route.
- Path and query params with types and constraints.
- Request body schema.
- Response schema per status code.
- Validation rules.
- Handler behavior — numbered steps. Be explicit; the skill executes these
  steps literally.

See [`example/contracts/users-api.yaml`](./example/contracts/users-api.yaml)
for the UserService equivalent.

### 5. `tasks.md` — executable task list

The ordered to-do the agent chews through. Each task is one action with
clear inputs and a clear done-criterion. Format:

```markdown
- [ ] **T001** Scaffold solution + project references + `dotnet build` passes
- [ ] **T002** Generate `User` entity from `data-model.md`
- [ ] **T003** Generate `IUserRepository` with `GetByEmailAsync`
- [ ] **T004** Generate user endpoints from `contracts/users-api.yaml`
- [ ] **T005** Implement email-uniqueness rule from `spec.md` §Business rules
- [ ] **T006** Copy tests from `tests/` into `tests/UnitTests` / `tests/IntegrationTests` and run `dotnet test`
```

The skill checks each box as it finishes the task. If `dotnet build` or
`dotnet test` fails on a task, the skill stops and reports — it does **not**
skip to the next task.

---

## Rules of thumb

1. **One feature per `specs/<slug>/` directory.** For the initial scaffold,
   use `specs/001-initial-service/`. Follow-on features get `002-...`, etc.
2. **Be exact about types and nullability.** `string` vs `string?` matters.
3. **Don't describe implementation.** Say *what*, not *how*. No LINQ, no
   method bodies — those are output, not input.
4. **If a field isn't in `data-model.md`, it isn't in the code.** The skill
   refuses to invent. Accept this.
5. **Tests are the acceptance contract.** The skill will not edit them to
   make them pass. If a test fails, the production code is wrong (or the
   test is, and you should say so explicitly).
6. **Keep the spec in the generated repo.** Re-running the skill after
   editing the spec keeps spec and code in sync.

---

## When to re-run the skill

- You changed the spec and want to regenerate. The skill overwrites only
  files it owns (entities, repositories, DTOs, validators, commands, queries,
  handlers, endpoint registration, `catalog-info.yaml`). Edits you've made
  by hand to `Program.cs` outside the marker comments, to `Middleware/`, or
  to tests, are preserved.
- You added tests to `tests/` and want them run.
- You amended the `constitution.md` and want to verify this service still
  conforms.

## Using just one file (the "starter" shape)

If you're early and the decomposition feels heavy, start with a single
`spec.md` that has all five sections inline. The skill accepts either layout:

- `specs/001-initial-service/spec.md` only (everything inline), or
- `specs/001-initial-service/{spec.md, plan.md, data-model.md, contracts/, tasks.md, tests/}` (decomposed).

Upgrade to the decomposed layout when the monolithic file crosses ~300 lines
or when multiple people start editing it in parallel. See
[`../spec-template.md`](./spec-template.md) for the monolithic form.

---

## Reference material

- [GitHub Spec Kit](https://github.com/github/spec-kit) — canonical SDD
  tooling and templates. Worth reading `templates/spec-template.md` and
  `templates/plan-template.md` there for the reference shape.
- [AWS Kiro](https://kiro.dev/) — an IDE built around this workflow; useful
  for seeing the pattern with richer tooling.
- [Backstage Software Catalog + Scaffolder](https://backstage.io/docs/features/software-catalog/)
  — `catalog-info.yaml` is produced by this skill as one of its outputs, so
  the generated service is immediately registerable.
