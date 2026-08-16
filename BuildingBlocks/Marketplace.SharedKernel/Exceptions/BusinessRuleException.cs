namespace Marketplace.SharedKernel.Exceptions;

/// <summary>
/// Uma regra de negocio foi violada. Traduzida para <b>HTTP 409 Conflict</b>.
/// </summary>
/// <remarks>
/// <para>
/// A distincao em relacao ao 400 e importante e costuma ser cobrada em entrevista:
/// <list type="bullet">
///   <item><b>400 Bad Request</b>: a requisicao esta malformada — campo obrigatorio
///   ausente, e-mail invalido, quantidade negativa. Quem valida e o FluentValidation
///   no pipeline, antes mesmo de chegar ao handler.</item>
///   <item><b>409 Conflict</b>: a requisicao esta perfeitamente bem formada, mas
///   conflita com o estado atual do sistema — "e-mail ja cadastrado", "estoque
///   insuficiente", "pedido ja pago". So da para saber consultando os dados.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// if (await dbContext.Users.AnyAsync(u => u.Email == email, ct))
/// {
///     throw new BusinessRuleException("E-mail ja cadastrado.");
/// }
/// </code>
/// </example>
public sealed class BusinessRuleException : MarketplaceException
{
    /// <summary>
    /// Cria a excecao descrevendo a regra violada.
    /// </summary>
    /// <param name="message">Explicacao da regra, segura para exibir ao cliente.</param>
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
