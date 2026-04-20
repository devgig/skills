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
    public async Task SaveAsync_stamps_UpdatedAt_on_existing_user()
    {
        var user = new User { FirstName = "A", LastName = "B", Email = "a@b.com" };
        var saved = await _sut.SaveAsync(user);

        saved.LastName = "Changed";
        var updated = await _sut.SaveAsync(saved);

        updated.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetByEmailAsync_returns_null_when_not_found()
    {
        var result = await _sut.GetByEmailAsync("missing@example.com");
        result.Should().BeNull();
    }

    public void Dispose() => _runner.Dispose();
}
