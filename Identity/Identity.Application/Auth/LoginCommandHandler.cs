using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Marketplace.SharedKernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

/// <summary>
/// Autentica um usuario existente e devolve um novo par de tokens.
/// </summary>
/// <remarks>
/// <para>
/// <b>Duas decisoes de seguranca visiveis no codigo:</b>
/// </para>
/// <list type="number">
///   <item><b>Resposta uniforme.</b> Usuario inexistente e senha errada produzem a mesma
///   <see cref="AuthenticationFailedException"/> com a mesma mensagem. Diferenciar os
///   dois casos permitiria a um atacante descobrir quais e-mails tem conta.</item>
///   <item><b>Verificacao de hash sempre executada.</b> Quando o usuario nao existe,
///   ainda assim o BCrypt roda contra um hash falso. Sem isso, a resposta para e-mail
///   inexistente voltaria em ~1 ms e para e-mail existente em ~100 ms — e essa
///   diferenca de tempo entrega a informacao que a mensagem uniforme tentava esconder
///   (ataque de canal lateral por temporizacao).</item>
/// </list>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Identity.</param>
/// <param name="passwordHasher">Servico de hash de senha.</param>
/// <param name="tokenService">Servico de geracao de tokens.</param>
public sealed class LoginCommandHandler(
    IIdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<LoginCommand, AuthResponse>
{
    /// <summary>
    /// Hash BCrypt descartavel, usado apenas para igualar o tempo de resposta quando o
    /// e-mail informado nao existe. Corresponde a uma senha aleatoria sem valor.
    /// </summary>
    private const string DummyPasswordHash = "$2a$11$C6UzMDM.H6dfI/f/IKcEe.3S1cM8Kj6.M1GC0QpJlDXqbJ0Fx8Cwm";

    /// <inheritdoc />
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .Include(candidate => candidate.RefreshTokens)
            .FirstOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        // A verificacao roda mesmo com user == null: e o que mantem o tempo de resposta
        // constante entre "e-mail nao existe" e "senha errada".
        var passwordMatches = passwordHasher.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null || !passwordMatches)
        {
            throw new AuthenticationFailedException();
        }

        var tokens = tokenService.Generate(user);
        user.AddRefreshToken(tokenService.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAtUtc);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAtUtc);
    }
}
