using System.Text.Json;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using Marketplace.SharedKernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth;

/// <summary>
/// Cadastra um novo usuario e anuncia o fato aos demais servicos via outbox.
/// </summary>
/// <remarks>
/// <para>
/// Fluxo do caso de uso:
/// </para>
/// <list type="number">
///   <item>normaliza o e-mail (uma unica vez, na entidade);</item>
///   <item>verifica duplicidade usando <b>exatamente o valor normalizado</b>;</item>
///   <item>cria o usuario com a senha ja convertida em hash BCrypt;</item>
///   <item>emite o par de tokens e grava o <i>hash</i> do refresh token;</item>
///   <item>enfileira <c>UserCreatedEvent</c> no outbox;</item>
///   <item>um unico <c>SaveChangesAsync</c> grava usuario + token + evento.</item>
/// </list>
/// <para>
/// <b>Bug corrigido no passo 2.</b> A versao anterior comparava com
/// <c>request.Email</c> cru e gravava a versao normalizada, entao
/// <c>"Ana@Teste.com"</c> passava pela checagem depois de <c>"ana@teste.com"</c> ja
/// existir — e o erro so aparecia como violacao de indice unico no Postgres,
/// devolvendo 500 ao cliente. Agora a checagem e a gravacao usam o mesmo valor.
/// </para>
/// <para>
/// <b>Nota sobre concorrencia.</b> Mesmo com a checagem correta, dois cadastros
/// simultaneos do mesmo e-mail podem passar juntos pelo <c>AnyAsync</c>. O indice unico
/// no banco continua sendo a garantia final — a checagem aqui existe para transformar o
/// caso comum numa mensagem clara, nao para substituir a restricao do banco.
/// </para>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Identity.</param>
/// <param name="passwordHasher">Servico de hash de senha.</param>
/// <param name="tokenService">Servico de geracao de tokens.</param>
public sealed class RegisterUserCommandHandler(
    IIdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    /// <inheritdoc />
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(request.Email);

        var emailAlreadyUsed = await dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailAlreadyUsed)
        {
            throw new BusinessRuleException("E-mail ja cadastrado.");
        }

        var user = new User(Guid.NewGuid(), request.Name, normalizedEmail, passwordHasher.Hash(request.Password));

        var tokens = tokenService.Generate(user);
        user.AddRefreshToken(tokenService.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAtUtc);

        await dbContext.Users.AddAsync(user, cancellationToken);

        // Outbox: o evento entra na MESMA transacao do usuario. Ou os dois sao gravados,
        // ou nenhum — nunca "usuario criado sem e-mail de boas-vindas".
        await dbContext.OutboxMessages.AddAsync(
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(UserCreatedEvent).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(
                    new UserCreatedEvent(Guid.NewGuid(), user.Id, user.Name, user.Email, user.CreatedAtUtc)),
                OccurredOnUtc = DateTime.UtcNow
            },
            cancellationToken);

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
