# Docs — scaffold-csharp-microservice

This folder tells you how to drive the skill.

| File | What's in it |
|---|---|
| [`how-to-write-a-prp.md`](./how-to-write-a-prp.md) | Guide to writing a Product Requirements Prompt — what each section is for, why it's structured that way, rules of thumb. **Read first.** |
| [`prp-template.md`](./prp-template.md) | Empty PRP template. Copy to your new repo as `prp.md`, fill it in, run the skill. |
| [`example-prp.md`](./example-prp.md) | A fully-worked PRP that reverse-engineers the existing UserService. Open this side-by-side with `~/repos/UserService` to see how each PRP section projects into real code. |

## Quick start

1. Read `how-to-write-a-prp.md`.
2. Copy `prp-template.md` to your new service's repo root as `prp.md`.
3. Fill every section. When stuck, check `example-prp.md` for what "complete" looks like.
4. (Optional) Drop xUnit test files into `../tests-inbox/` or paste them inline under the PRP's **Tests** section.
5. Invoke the skill: tell Claude "scaffold a C# microservice from my PRP at `<path>`" (or type `/scaffold-csharp-microservice`).
6. Claude walks through seven phases — read & validate PRP → scaffold → entities → endpoints → business rules → run tests → report. It pauses after each phase if `dotnet build` fails.

## Where your custom logic goes

| What you want to specify | Where it goes in the PRP |
|---|---|
| Service identity (name, DB name) | Section 1 |
| Database shape (entities, fields, indexes) | Section 2 — **Entities** |
| Custom queries beyond CRUD | Section 2 — **Query methods** table |
| HTTP routes + verbs | Section 3 — each `### Endpoint:` block |
| Request/response DTO shape | Section 3 — **Request body** / **Response body** tables |
| Input validation rules | Section 3 — **Validation** section (also Section 2 for entity-level rules) |
| What a handler does step-by-step | Section 3 — **Handler behavior** numbered list |
| Cross-handler / domain-wide rules | Section 4 — **Business rules** |
| Acceptance tests | Section 5 — **Tests** (inline or by reference) |

Anything the PRP doesn't declare, the skill won't invent. That's the contract.
