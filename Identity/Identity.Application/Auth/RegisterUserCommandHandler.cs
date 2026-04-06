using Identity.Application.Abstractions;
using System.Text.Json;
using Identity.Domain.Entities;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

public sealed class RegisterUserCommandHandler(
    IIdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await dbContext.Users.AnyAsync(user => user.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User(Guid.NewGuid(), request.Name, request.Email.Trim().ToLowerInvariant(), passwordHasher.Hash(request.Password));
        var tokens = tokenService.Generate(user);
        user.AddRefreshToken(tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.OutboxMessages.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(UserCreatedEvent).AssemblyQualifiedName ?? typeof(UserCreatedEvent).FullName ?? nameof(UserCreatedEvent),
            Payload = JsonSerializer.Serialize(new UserCreatedEvent(Guid.NewGuid(), user.Id, user.Name, user.Email, user.CreatedAt)),
            OccurredOnUtc = DateTime.UtcNow
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Name, user.Email, tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
    }
}