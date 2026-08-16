namespace Marketplace.SharedKernel.Exceptions;

/// <summary>
/// Excecao base de todas as falhas de negocio previstas pelo marketplace.
/// </summary>
/// <remarks>
/// <para>
/// Por que uma hierarquia propria em vez de <see cref="InvalidOperationException"/>?
/// Antes, o middleware de erros traduzia qualquer <c>InvalidOperationException</c> em
/// HTTP 400. Isso e perigoso: o proprio framework lanca esse tipo em situacoes
/// completamente diferentes (servico nao registrado no container, DbContext ja
/// descartado...), e um bug de infraestrutura acabava mascarado como "erro do cliente"
/// com status 400 — escondendo o problema real do time de operacao.
/// </para>
/// <para>
/// Com tipos proprios, o middleware mapeia deliberadamente:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> → 404</item>
///   <item><see cref="BusinessRuleException"/> → 409</item>
///   <item><see cref="ForbiddenAccessException"/> → 403</item>
///   <item>qualquer outra excecao → 500 (e um alerta no log)</item>
/// </list>
/// </para>
/// </remarks>
public abstract class MarketplaceException : Exception
{
    /// <summary>
    /// Inicializa a excecao com a mensagem informada.
    /// </summary>
    /// <param name="message">Mensagem segura para ser devolvida ao cliente da API.</param>
    protected MarketplaceException(string message)
        : base(message)
    {
    }
}
