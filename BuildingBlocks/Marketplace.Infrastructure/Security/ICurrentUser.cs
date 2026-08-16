namespace Marketplace.Infrastructure.Security;

/// <summary>
/// Abstracao do usuario autenticado na requisicao atual.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que nao injetar <c>IHttpContextAccessor</c> direto no handler?</b> Porque isso
/// amarraria a camada de aplicacao ao ASP.NET Core. Com esta interface, o handler
/// depende de um contrato de uma propriedade so — e o teste unitario passa um duble
/// em vez de montar um <c>HttpContext</c> falso.
/// </para>
/// <para>
/// <b>Por que isso e critico de seguranca?</b> Sem ela, o identificador do usuario
/// chegaria pelo corpo da requisicao. Qualquer cliente autenticado poderia enviar o
/// <c>userId</c> de outra pessoa e ler o carrinho ou criar pedidos em nome dela
/// (a falha conhecida como <i>IDOR</i>, Insecure Direct Object Reference). O
/// identificador vem sempre do token assinado, que o cliente nao consegue forjar.
/// </para>
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// Identificador do usuario autenticado, extraido da claim <c>sub</c> do JWT.
    /// </summary>
    /// <value><see langword="null"/> quando a requisicao e anonima.</value>
    Guid? UserId { get; }

    /// <summary>
    /// Indica se a requisicao atual esta autenticada.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Devolve o identificador do usuario autenticado ou falha.
    /// </summary>
    /// <returns>Identificador do usuario.</returns>
    /// <exception cref="SharedKernel.Exceptions.ForbiddenAccessException">
    /// Lancada quando a requisicao nao esta autenticada.
    /// </exception>
    Guid RequireUserId();

    /// <summary>
    /// Garante que o usuario autenticado e o dono do recurso solicitado.
    /// </summary>
    /// <param name="resourceOwnerId">Identificador do dono do recurso.</param>
    /// <exception cref="SharedKernel.Exceptions.ForbiddenAccessException">
    /// Lancada quando o usuario autenticado e diferente do dono do recurso.
    /// </exception>
    void EnsureOwns(Guid resourceOwnerId);
}
