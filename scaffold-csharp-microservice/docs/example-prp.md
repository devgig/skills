# Example PRP — UserService (reverse-engineered)

> This PRP, if fed back through `scaffold-csharp-microservice`, would produce a
> service functionally equivalent to the existing UserService reference repo
> at `~/repos/UserService`. Use it as a working example of how PRP sections
> translate into a real codebase. Compare each section below to the matching
> file in UserService to see the projection in action.

---

## 1. Service

- **Service name (PascalCase):** `UserService`
- **Kebab name:** `user-service`
- **Database name:** `UserServiceDb`
- **Target directory:** `/Users/me/repos/UserService`
- **Consumer (Kafka)?** no (the real repo has one; this PRP omits it for brevity)

---

## 2. Entities

### Entity: User

- **Collection name:** `Users`

**Fields**

| Field name | C# type    | Nullable? | Default      | BSON name (if different) | Notes                                 |
|------------|------------|-----------|--------------|--------------------------|---------------------------------------|
| Id         | string     | no        | empty string | _id                      | `[BsonId][BsonRepresentation(ObjectId)]` |
| FirstName  | string     | no        | empty string |                          |                                       |
| LastName   | string     | no        | empty string |                          |                                       |
| Email      | string     | no        | empty string |                          |                                       |
| CreatedAt  | DateTime   | no        | `default`    |                          | Set by `SaveAsync` on create          |
| UpdatedAt  | DateTime?  | yes       | null         |                          | Set by `SaveAsync` on update          |

**Indexes**

- None declared in the reference repo. (A production service should add a unique index on `Email`; omit here to match what's actually there.)

**Query methods** (on top of the inherited `BaseRepository<T>` API)

| Method signature                                         | Returns           | Behavior                                                                                |
|----------------------------------------------------------|-------------------|-----------------------------------------------------------------------------------------|
| `GetByEmailAsync(string email)`                          | `Task<User?>`     | Find one by email (case-sensitive); null if missing                                     |
| `SaveAsync(User user)`                                   | `Task<User>`      | If `Id` empty → set `CreatedAt = UtcNow` then `CreateAsync`. Else → set `UpdatedAt = UtcNow` then `UpdateAsync`. |

**Validation notes**

- FirstName: required, max 50 characters
- LastName: required, max 50 characters
- Email: required, valid email format, max 100 characters

---

## 3. Endpoints

### Endpoint: CreateUser

- **Method:** POST
- **Route:** `/api/user`
- **Name:** `CreateUser`
- **Path params:** none
- **Query params:** none
- **Request body:**

  | Field     | Type   | Required? | Notes                       |
  |-----------|--------|-----------|-----------------------------|
  | FirstName | string | yes       | max 50                      |
  | LastName  | string | yes       | max 50                      |
  | Email     | string | yes       | valid email format, max 100 |

- **Response body (201):**

  | Field     | Type     | Notes                               |
  |-----------|----------|-------------------------------------|
  | Id        | Guid     | new user id (parsed from ObjectId)  |
  | FirstName | string   |                                     |
  | LastName  | string   |                                     |
  | Email     | string   |                                     |
  | CreatedAt | DateTime |                                     |

- **Status codes:** 201 Created, 400 ValidationProblem, 500
- **Validation:** FirstName/LastName/Email rules from the User entity validation notes.
- **Handler behavior:**
  1. Call `IUserRepository.GetByEmailAsync(request.Email)`. If non-null, throw `InvalidOperationException($"User with email {email} already exists")` — maps to 409 via the GlobalExceptionHandler.
  2. Create a new `User` from the request fields.
  3. Call `_userRepository.SaveAsync(user)` to persist and stamp `CreatedAt`.
  4. Return a `CreateUserResponse` with `Guid.Parse(savedUser.Id)` and the saved fields.

---

### Endpoint: GetUserById

- **Method:** GET
- **Route:** `/api/user/{id:guid}`
- **Name:** `GetUserById`
- **Path params:** `id` (Guid)
- **Request body:** none
- **Response body (200):** `GetUserByIdResponse` — same shape as `CreateUserResponse`.
- **Status codes:** 200 OK, 404 NotFound, 500
- **Validation:** none (path param only)
- **Handler behavior:** Load by id; throw `KeyNotFoundException` if null — maps to 404.

---

### Endpoint: GetUserByEmail

- **Method:** GET
- **Route:** `/api/user/by-email/{email}`
- **Name:** `GetUserByEmail`
- **Path params:** `email` (string)
- **Request body:** none
- **Response body (200):** `GetUserByEmailResponse` — same shape as `CreateUserResponse`.
- **Status codes:** 200 OK, 404 NotFound, 500
- **Validation:** none
- **Handler behavior:** `IUserRepository.GetByEmailAsync(email)`; throw `KeyNotFoundException` if null.

---

### Endpoint: GetAllUsers

- **Method:** GET
- **Route:** `/api/user`
- **Name:** `GetAllUsers`
- **Query params:**

  | Name       | Type | Required? | Default | Notes             |
  |------------|------|-----------|---------|-------------------|
  | PageNumber | int  | no        | 1       | must be >= 1      |
  | PageSize   | int  | no        | 20      | must be 1..100    |

- **Response body (200):** `GetAllUsersResponse` — `Items` (list of user DTO), `TotalCount`, `PageNumber`, `PageSize`.
- **Status codes:** 200 OK, 400 ValidationProblem, 500
- **Validation:** PageNumber >= 1; PageSize between 1 and 100.
- **Handler behavior:** Paginated fetch. Use `GetAllAsync` then skip/take (the reference does in-memory pagination — acceptable here since not performance-critical).

---

### Endpoint: UpdateUser

- **Method:** PUT
- **Route:** `/api/user/{id:guid}`
- **Name:** `UpdateUser`
- **Path params:** `id` (Guid)
- **Request body:** same fields as `CreateUserRequest`.
- **Response body (200):** `UpdateUserResponse` — same shape as `CreateUserResponse` plus `UpdatedAt`.
- **Status codes:** 200 OK, 400 ValidationProblem, 404 NotFound, 500
- **Validation:** same rules as `CreateUserRequest`.
- **Handler behavior:**
  1. Load by id; throw `KeyNotFoundException` if null.
  2. Apply `FirstName`, `LastName`, `Email` from the request.
  3. `SaveAsync` (stamps `UpdatedAt`).
  4. Return the updated record.

---

### Endpoint: PatchUser

- **Method:** PATCH
- **Route:** `/api/user/{id:guid}`
- **Name:** `PatchUser`
- **Behavior identical to `UpdateUser`** in the reference (it does not support partial updates — this is intentional parity with the existing code; a real PATCH would apply only provided fields).

---

### Endpoint: DeleteUser

- **Method:** DELETE
- **Route:** `/api/user/{id:guid}`
- **Name:** `DeleteUser`
- **Path params:** `id` (Guid)
- **Request body:** none
- **Response body:** none
- **Status codes:** 204 NoContent, 404 NotFound, 500
- **Validation:** none
- **Handler behavior:** `_userRepository.DeleteAsync(id.ToString())`. The reference doesn't verify existence first — match that.

---

## 4. Business rules

- **Email uniqueness on create.** No two users may share an email address.
  Enforced in `CreateUserCommandHandler` by `GetByEmailAsync` before insert.
  Throws `InvalidOperationException` → 409 Conflict via `GlobalExceptionHandler`.
  *(Note: the reference does **not** enforce uniqueness on update — matching
  that behavior here is intentional. A production service should close that
  gap.)*

- **Timestamp stamping.** `CreatedAt` is set on create; `UpdatedAt` is set on
  every update. Implemented in `UserRepository.SaveAsync` (branches on whether
  `Id` is empty).

---

## 5. Tests

### Test: CreateUserCommandHandlerTests.cs

```csharp
using Application.Commands;
using Application.Handlers;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace UnitTests.Application.Handlers;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();

    [Fact]
    public async Task Handle_returns_response_when_email_is_unique()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync((User?)null);
        _repo.Setup(r => r.SaveAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) => { u.Id = "507f1f77bcf86cd799439011"; u.CreatedAt = DateTime.UtcNow; return u; });

        var handler = new CreateUserCommandHandler(_repo.Object);
        var cmd = new CreateUserCommand("Ada", "Lovelace", "ada@example.com");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FirstName.Should().Be("Ada");
        result.Email.Should().Be("ada@example.com");
        _repo.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_email_already_exists()
    {
        _repo.Setup(r => r.GetByEmailAsync("dup@example.com"))
             .ReturnsAsync(new User { Email = "dup@example.com" });

        var handler = new CreateUserCommandHandler(_repo.Object);
        var cmd = new CreateUserCommand("A", "B", "dup@example.com");

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
        _repo.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }
}
```

### Test: UserRepositoryIntegrationTests.cs

```csharp
using Domain.Entities;
using Domain.Repositories;
using EphemeralMongo;
using FluentAssertions;
using MongoDB.Driver;

namespace IntegrationTests;

public class UserRepositoryIntegrationTests : IDisposable
{
    private readonly IMongoRunner _runner;
    private readonly UserRepository _sut;

    public UserRepositoryIntegrationTests()
    {
        _runner = MongoRunner.Run();
        var client = new MongoClient(_runner.ConnectionString);
        var db = client.GetDatabase("UserServiceDb_test");
        _sut = new UserRepository(db);
    }

    [Fact]
    public async Task SaveAsync_stamps_CreatedAt_on_new_user()
    {
        var user = new User { FirstName = "A", LastName = "B", Email = "a@b.com" };

        var saved = await _sut.SaveAsync(user);

        saved.Id.Should().NotBeNullOrEmpty();
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        saved.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_returns_null_when_not_found()
    {
        var result = await _sut.GetByEmailAsync("missing@example.com");
        result.Should().BeNull();
    }

    public void Dispose() => _runner.Dispose();
}
```

---

## How to read this example

- **Section 1** becomes the solution name, Dockerfile labels, and
  OpenTelemetry `ServiceName`. Open `src/Service/appsettings.json` in the real
  UserService and match lines to this section.
- **Section 2** becomes `src/Domain/Entities/User.cs`,
  `src/Domain/Repositories/IUserRepository.cs`, and `UserRepository.cs`. The
  query method table becomes interface methods; the BSON attributes come from
  the Fields table's `BSON name` + `Notes` columns.
- **Section 3** becomes `src/Service/Endpoints/UserEndpoints.cs`,
  `src/Service/Models/*.cs`, `src/Service/Validators/*.cs`,
  `src/Application/Commands/*.cs`, `src/Application/Queries/*.cs`, and
  `src/Application/Handlers/*.cs`. One endpoint block = one full vertical
  slice.
- **Section 4** becomes code inside the handlers from Section 3. The skill
  won't guess these rules — they have to be spelled out here.
- **Section 5** becomes files dropped into
  `tests/UnitTests/Application/Handlers/` and `tests/IntegrationTests/`. The
  skill runs them and gates scaffold success on their passing.

If you copy this file to `~/repos/UserService/prp.md` and run the skill
against an empty target, you'll get a close cousin of the current
UserService. Small gaps you'll notice (deliberate for this example): no
Kafka consumer, no `Mappers` folder, no `Application.Services.UserService`
wrapper — those are additions the real repo grew organically and aren't part
of the core pattern.
