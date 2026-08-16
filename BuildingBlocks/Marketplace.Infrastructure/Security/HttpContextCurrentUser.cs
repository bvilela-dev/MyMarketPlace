using System.Security.Claims;
using Marketplace.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Marketplace.Infrastructure.Security;

/// <summary>
/// Implementacao de <see cref="ICurrentUser"/> que le as claims do <c>HttpContext</c>.
/// </summary>
/// <param name="httpContextAccessor">Acesso ao contexto HTTP da requisicao em andamento.</param>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Pegadinha classica do ASP.NET Core: por padrao o handler do JWT Bearer
            // "traduz" as claims curtas do padrao JWT para as URIs longas do WS-Security,
            // e "sub" vira ClaimTypes.NameIdentifier. Neste projeto esse mapeamento e
            // desligado (MapInboundClaims = false), mas os dois nomes sao consultados
            // para que a classe funcione mesmo se alguem religar o mapeamento.
            var subject = principal.FindFirstValue("sub")
                          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => UserId is not null;

    /// <inheritdoc />
    public Guid RequireUserId()
        => UserId ?? throw new ForbiddenAccessException("Requisicao sem usuario autenticado valido.");

    /// <inheritdoc />
    public void EnsureOwns(Guid resourceOwnerId)
    {
        if (RequireUserId() != resourceOwnerId)
        {
            // A mensagem e deliberadamente generica: dizer "este carrinho e do usuario X"
            // confirmaria a existencia do recurso alheio para um atacante.
            throw new ForbiddenAccessException();
        }
    }
}
