namespace Marketplace.SharedKernel.Exceptions;

/// <summary>
/// Recurso solicitado nao existe. Traduzida para <b>HTTP 404 Not Found</b>.
/// </summary>
/// <example>
/// <code>
/// var user = await dbContext.Users.FindAsync(id, ct)
///     ?? throw new NotFoundException("Usuario", id);
/// </code>
/// </example>
public sealed class NotFoundException : MarketplaceException
{
    /// <summary>
    /// Cria a excecao com uma mensagem livre.
    /// </summary>
    /// <param name="message">Descricao do recurso ausente.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Cria a excecao com a mensagem padronizada "{recurso} '{chave}' nao foi encontrado.".
    /// </summary>
    /// <param name="resource">Nome do recurso (ex.: <c>"Pedido"</c>).</param>
    /// <param name="key">Chave usada na busca.</param>
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' nao foi encontrado.")
    {
    }
}
