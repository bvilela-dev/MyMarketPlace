namespace Marketplace.SharedKernel.Exceptions;

/// <summary>
/// Usuario autenticado tentando acessar um recurso que nao lhe pertence.
/// Traduzida para <b>HTTP 403 Forbidden</b>.
/// </summary>
/// <remarks>
/// <para>
/// 401 x 403 e outra confusao comum:
/// <list type="bullet">
///   <item><b>401 Unauthorized</b>: "nao sei quem voce e" — token ausente, invalido ou
///   expirado. Quem responde e o middleware de autenticacao.</item>
///   <item><b>403 Forbidden</b>: "sei quem voce e, e voce nao pode fazer isso" — o token
///   e valido, mas o recurso e de outra pessoa.</item>
/// </list>
/// </para>
/// <para>
/// Esta excecao cobre a falha de autorizacao <b>por instancia de recurso</b>, que
/// nenhum atributo <c>[Authorize]</c> consegue expressar: so olhando o dado da que se
/// sabe se o carrinho <c>{userId}</c> pertence a quem enviou o token.
/// </para>
/// </remarks>
public sealed class ForbiddenAccessException : MarketplaceException
{
    /// <summary>
    /// Cria a excecao com a mensagem padrao de acesso negado.
    /// </summary>
    public ForbiddenAccessException()
        : base("Voce nao tem permissao para acessar este recurso.")
    {
    }

    /// <summary>
    /// Cria a excecao com uma mensagem especifica.
    /// </summary>
    /// <param name="message">Motivo da negativa (sem vazar dados de terceiros).</param>
    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
