# Docs — scaffold-csharp-microservice

This folder tells you how to drive the skill, which follows
**Spec-Driven Development** (the
[GitHub Spec Kit](https://github.com/github/spec-kit) pattern).

| File | What's in it |
|---|---|
| [`how-to-write-a-spec.md`](./how-to-write-a-spec.md) | Guide to writing a spec — what each artifact (`spec.md`, `plan.md`, `data-model.md`, `contracts/`, `tasks.md`, `tests/`) is for, and when to decompose vs. stay monolithic. **Read first.** |
| [`spec-template.md`](./spec-template.md) | Monolithic starter template — use when the service is small enough for one file. |
| [`example/`](./example/) | Fully-worked example reverse-engineered from the existing UserService, in the full decomposed layout. See [`example/README.md`](./example/README.md) for the guided tour. |

The platform-level constitution is at
[`../constitution.md`](../constitution.md) — the non-negotiables every
service scaffolded by this skill obeys.

## Quick start

1. Read [`how-to-write-a-spec.md`](./how-to-write-a-spec.md).
2. Decide: monolithic or decomposed?
   - **Monolithic** (one file, all sections inline) — copy
     [`spec-template.md`](./spec-template.md) to your new repo as
     `spec.md` and fill it in.
   - **Decomposed** (multiple files) — create
     `specs/001-initial-service/` in your new repo and copy the structure
     from [`example/`](./example/), replacing UserService content with
     your own.
3. (Optional) drop xUnit test files into the spec's `tests/` folder or
   into `../tests-inbox/`.
4. Invoke the skill: "scaffold a C# microservice from `<path-to-spec>`",
   or type `/scaffold-csharp-microservice`.
5. Claude walks eight phases: constitution check → read spec → scaffold
   base → entities → endpoints → business rules → Backstage descriptor →
   run tests → report. Each phase gates on `dotnet build` / `dotnet test`.

## Where your custom logic goes

| What you want to specify | Where it goes | Example in `example/` |
|---|---|---|
| Service identity (name, owner, Backstage system) | `spec.md` §Service | [`example/spec.md`](./example/spec.md) |
| User stories and scope | `spec.md` §User stories / §Scope | [`example/spec.md`](./example/spec.md) |
| Architecture + tech choices | `plan.md` | [`example/plan.md`](./example/plan.md) |
| Database shape (entities, fields, indexes, queries) | `data-model.md` | [`example/data-model.md`](./example/data-model.md) |
| HTTP routes, DTOs, validation, handler behavior | `contracts/<resource>.yaml` | [`example/contracts/users-api.yaml`](./example/contracts/users-api.yaml) |
| Cross-handler invariants, domain rules | `spec.md` §Business rules | [`example/spec.md`](./example/spec.md#business-rules) |
| Agent task list | `tasks.md` | [`example/tasks.md`](./example/tasks.md) |
| Acceptance tests | `tests/<Feature>Tests.cs` | [`example/tests/`](./example/tests/) |
| Backstage descriptor | *generated* from `spec.md` | [`example/catalog-info.yaml`](./example/catalog-info.yaml) |

Anything the spec doesn't declare, the skill won't invent. That's the
contract.

## Vocabulary — why "spec" and not "PRP"?

The industry has converged on **spec** as the term for SDD input files,
driven by Spec Kit, AWS Kiro, and Thoughtworks' writing on the subject.
"PRP" is not a recognized term. Using the standard vocabulary makes specs
portable across tools (Spec Kit, Kiro, plain markdown) and makes onboarding
easier — engineers already know what a `spec.md` is.
