# data-model.md — UserService

One entity: `User`. Repository inherits the generic `BaseRepository<T>` and
adds two custom methods.

---

## Entity: User

- **Collection:** `Users`
- **Primary key:** `Id` — string-serialized MongoDB `ObjectId`.

### Fields

| Field name | C# type   | Nullable? | Default        | BSON name | Notes |
|------------|-----------|-----------|----------------|-----------|-------|
| Id         | string    | no        | `string.Empty` | `_id`     | `[BsonId][BsonRepresentation(BsonType.ObjectId)]` |
| FirstName  | string    | no        | `string.Empty` |           | max 50 chars (enforced at API boundary) |
| LastName   | string    | no        | `string.Empty` |           | max 50 chars |
| Email      | string    | no        | `string.Empty` |           | valid email format, max 100 chars |
| CreatedAt  | DateTime  | no        | `default`      |           | stamped by `SaveAsync` on insert |
| UpdatedAt  | DateTime? | yes       | `null`         |           | stamped by `SaveAsync` on replace |

### Indexes

- **None declared** in the reference repo.
  > Known gap: a production deployment should add a unique index on `Email`
  > to enforce business rule **B-1** at the database level (not just in
  > `CreateUserCommandHandler`). Omitted here to match what the real service
  > actually does.

### State transitions

None. `User` has no lifecycle states; records exist until deleted.

### Query methods (in addition to inherited `BaseRepository<T>`)

| Method signature                              | Returns        | Behavior |
|-----------------------------------------------|----------------|----------|
| `GetByEmailAsync(string email)`               | `Task<User?>`  | Find one document where `Email == email` (case-sensitive). Returns null if none. |
| `SaveAsync(User user)`                        | `Task<User>`   | If `Id` is empty → set `CreatedAt = UtcNow`, call `CreateAsync`. Else → set `UpdatedAt = UtcNow`, call `UpdateAsync`. Returns the saved entity. |

### Inherited (do not list)

`GetByIdAsync`, `GetAllAsync`, `FindAsync`, `FindOneAsync`, `CreateAsync`,
`UpdateAsync`, `DeleteAsync(id)`, `DeleteAsync(entity)`, `CountAsync`,
`ExistsAsync` — all provided by `BaseRepository<T>`.

### Validation notes (API-boundary rules)

Consumed by the endpoint contracts in
[`contracts/users-api.yaml`](./contracts/users-api.yaml); repeated here so
the data model is self-describing.

- `FirstName`, `LastName`: required; max 50 chars.
- `Email`: required; valid email format; max 100 chars.
- Business rule **B-1** (email uniqueness) enforced in
  `CreateUserCommandHandler` — see [`spec.md`](./spec.md#business-rules).

### Example document

```json
{
  "_id": { "$oid": "507f1f77bcf86cd799439011" },
  "FirstName": "Ada",
  "LastName": "Lovelace",
  "Email": "ada@example.com",
  "CreatedAt": { "$date": "2026-04-20T12:00:00Z" },
  "UpdatedAt": null
}
```
