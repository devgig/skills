# tests-inbox

Drop xUnit test files (`.cs`) here that you want the skill to run against the
generated service. When the skill reaches **Phase 6 — Run user-provided tests**,
it picks up every file in this folder, routes each to the right test project
(`UnitTests` or `IntegrationTests`), runs `dotnet test`, and reports results.

Filename convention: `<Feature>Tests.cs` — e.g. `CreateUserCommandHandlerTests.cs`.

These tests are the **acceptance contract**. The skill will not modify them to
make them pass — if a test fails, the production code is wrong (or the test
is, and you should say so). Claude will propose a fix to the production code
and ask before applying it.

Alternatives:

- You can list tests inline inside the PRP's **Tests** section (with the test
  file content in a fenced code block). The skill will save each block here
  automatically before running.
- You can point the skill at a different folder by saying "use tests from
  `<path>`" when invoking the skill.
