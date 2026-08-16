namespace Marketplace.SharedKernel.Exceptions;

/// <summary>
/// Falha na autenticacao do usuario. Traduzida para <b>HTTP 401 Unauthorized</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>A mensagem e propositalmente vaga.</b> Responder "e-mail nao cadastrado" para um
/// e-mail e "senha incorreta" para outro entrega ao atacante um oraculo de enumeracao
/// de contas: basta varrer uma lista de e-mails para descobrir quem tem cadastro. Por
/// isso os dois casos devolvem exatamente <c>"Credenciais invalidas."</c>.
/// </para>
/// <para>
/// Pelo mesmo motivo, o fluxo de login deveria ter custo de tempo parecido nos dois
/// casos — caso contrario a diferenca de latencia (verificar hash x nao verificar)
/// vira um canal lateral que revela a mesma informacao.
/// </para>
/// </remarks>
public sealed class AuthenticationFailedException : MarketplaceException
{
    /// <summary>
    /// Cria a excecao com a mensagem generica de credenciais invalidas.
    /// </summary>
    public AuthenticationFailedException()
        : base("Credenciais invalidas.")
    {
    }

    /// <summary>
    /// Cria a excecao com uma mensagem especifica.
    /// </summary>
    /// <param name="message">Mensagem — que nao deve revelar qual parte falhou.</param>
    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
