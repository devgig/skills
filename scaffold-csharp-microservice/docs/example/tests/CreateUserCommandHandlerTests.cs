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
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com"))
             .ReturnsAsync((User?)null);
        _repo.Setup(r => r.SaveAsync(It.IsAny<User>()))
             .ReturnsAsync((User u) =>
             {
                 u.Id = "507f1f77bcf86cd799439011";
                 u.CreatedAt = DateTime.UtcNow;
                 return u;
             });

        var handler = new CreateUserCommandHandler(_repo.Object);
        var cmd = new CreateUserCommand("Ada", "Lovelace", "ada@example.com");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FirstName.Should().Be("Ada");
        result.LastName.Should().Be("Lovelace");
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
