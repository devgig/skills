# tasks.md — UserService

Ordered task list the agent executes. Each task is self-contained and has a
clear done-criterion. The agent ticks a box only after its gate passes.

Gate rules:
- After every **T0nn** task, `dotnet build` at the solution root must return
  0 errors before moving on.
- After `T099`, `dotnet test` must return 0 failures.

---

## Phase A — Scaffold base

- [ ] **T001** Collect inputs from `spec.md` §Service: ServiceName=`UserService`, kebab=`user-service`, DatabaseName=`UserServiceDb`, target dir, Backstage system=`user-management`, owner=`dream-team`.
- [ ] **T002** Create target directory if missing. Fail if non-empty and overwrite not confirmed.
- [ ] **T003** Copy every file in `templates/` into the target dir, substituting `{{SERVICE_NAME}}`, `{{SERVICE_NAME_KEBAB}}`, `{{DATABASE_NAME}}`. Strip `.tmpl` from filenames.
- [ ] **T004** Run `dotnet new sln -n UserService` and `dotnet sln add src/*/*.csproj tests/*/*.csproj`.
- [ ] **T005** `dotnet build` at solution root. **Gate:** 0 errors.

## Phase B — Entities from `data-model.md`

- [ ] **T010** Generate `src/Domain/Entities/User.cs` with fields `Id, FirstName, LastName, Email, CreatedAt, UpdatedAt` and BSON attributes from `data-model.md`.
- [ ] **T011** Generate `src/Domain/Repositories/IUserRepository.cs` extending `IRepository<User>` and adding `GetByEmailAsync(string email)` and `SaveAsync(User user)`.
- [ ] **T012** Generate `src/Domain/Repositories/UserRepository.cs` inheriting `BaseRepository<User>` with collection name `Users`, implementing the two custom methods per `data-model.md`.
- [ ] **T013** Register the repository in `Program.cs` at the `// {{REPOSITORY_REGISTRATIONS}}` marker: `builder.Services.AddScoped<IUserRepository, UserRepository>();`.
- [ ] **T014** `dotnet build`. **Gate:** 0 errors.

## Phase C — Endpoints from `contracts/users-api.yaml`

For each operation in the contract file:

- [ ] **T020** `CreateUser` → DTOs, validator, `CreateUserCommand`, `CreateUserCommandHandler`, endpoint registration.
- [ ] **T021** `GetUserById` → DTOs, `GetUserByIdQuery`, handler, endpoint.
- [ ] **T022** `GetUserByEmail` → DTOs, `GetUserByEmailQuery`, handler, endpoint.
- [ ] **T023** `GetAllUsers` → DTOs, validator (PageNumber/PageSize), `GetAllUsersQuery`, handler, endpoint.
- [ ] **T024** `UpdateUser` → DTOs, validator, `UpdateUserCommand`, handler, endpoint.
- [ ] **T025** `PatchUser` → reuse `UpdateUserRequest`, endpoint only (handler behavior identical to `UpdateUser`).
- [ ] **T026** `DeleteUser` → `DeleteUserCommand`, handler, endpoint.
- [ ] **T027** Create `src/Service/Endpoints/UserEndpoints.cs` with `MapUserEndpoints` extension method; register `app.MapUserEndpoints()` in `Program.cs` at the `// {{ENDPOINT_REGISTRATIONS}}` marker.
- [ ] **T028** `dotnet build`. **Gate:** 0 errors.

## Phase D — Business rules from `spec.md`

- [ ] **T030** **B-1** Email uniqueness on create — verify `CreateUserCommandHandler` calls `GetByEmailAsync` and throws `InvalidOperationException` if non-null.
- [ ] **T031** **B-2** Timestamp stamping — verify `UserRepository.SaveAsync` branches on empty `Id` and stamps `CreatedAt` or `UpdatedAt`.
- [ ] **T032** **B-3** Id projection — verify response DTOs expose `Id` as `Guid` (parsed from the stored ObjectId string).
- [ ] **T033** `dotnet build`. **Gate:** 0 errors.

## Phase E — Backstage descriptor

- [ ] **T040** Generate `catalog-info.yaml` from the `spec.md` §Service fields and the operations in `contracts/users-api.yaml`. Emit `Component`, `API` (with the OpenAPI spec inlined), and `Resource: mongodb` records. See `example/catalog-info.yaml` for shape.

## Phase F — Tests

- [ ] **T099** Route tests:
  - `tests/CreateUserCommandHandlerTests.cs` → `tests/UnitTests/Application/Handlers/`.
  - `tests/UserRepositoryIntegrationTests.cs` → `tests/IntegrationTests/`.
- [ ] **T100** Run `dotnet test --logger "console;verbosity=normal"`. **Gate:** 0 failures.
- [ ] **T101** Print the final report: tree of generated files, endpoint table, entity summary, test results.

---

## Rules the agent follows while executing

- If a `dotnet build` gate fails, stop and report the error. Do not continue
  to the next task.
- If a test fails, propose a fix to the production code (not the test) and
  ask before applying.
- Never skip a task to work around a failure.
- Never modify a user-supplied test to make it pass.
