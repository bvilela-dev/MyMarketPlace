namespace Marketplace.Infrastructure.Security;

/// <summary>
/// Configuracao do JWT, lida da secao <c>"Jwt"</c> do appsettings/variaveis de ambiente.
/// </summary>
/// <remarks>
/// <para>
/// Esta classe vive nos building blocks (e nao dentro do Identity) porque os dois lados
/// da moeda precisam dela:
/// <list type="bullet">
///   <item>o <b>Identity</b> usa <see cref="Secret"/> para <i>assinar</i> o token;</item>
///   <item><b>Cart</b> e <b>Order</b> usam o mesmo <see cref="Secret"/> para
///   <i>validar</i> a assinatura, sem precisar chamar o Identity a cada requisicao.</item>
/// </list>
/// Se cada servico tivesse a sua propria copia da classe, uma divergencia de
/// <see cref="Issuer"/> so apareceria em runtime, como um 401 sem explicacao.
/// </para>
/// <para>
/// <b>Nota de seguranca — HS256 x RS256.</b> O projeto usa HMAC (chave simetrica), o que
/// significa que todo servico validador conhece a chave capaz de <i>emitir</i> tokens.
/// Em producao o correto e RSA/ECDSA (RS256): o Identity guarda a chave privada e os
/// demais servicos baixam apenas a chave publica via JWKS. Foi mantido simetrico aqui
/// para o ambiente de demonstracao rodar sem infraestrutura de chaves.
/// </para>
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>
    /// Nome da secao de configuracao correspondente.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Emissor esperado do token (claim <c>iss</c>).
    /// </summary>
    public string Issuer { get; set; } = "marketplace.identity";

    /// <summary>
    /// Publico-alvo esperado do token (claim <c>aud</c>).
    /// </summary>
    /// <remarks>
    /// Validar a audiencia evita que um token emitido para outro sistema — ainda que
    /// assinado com a mesma chave — seja aceito por este.
    /// </remarks>
    public string Audience { get; set; } = "marketplace.clients";

    /// <summary>
    /// Segredo simetrico usado para assinar e validar o token.
    /// </summary>
    /// <remarks>
    /// Precisa ter no minimo 32 bytes para HMAC-SHA256. Em producao vem sempre de um
    /// gerenciador de segredos (Kubernetes Secret, Vault, Key Vault) via variavel de
    /// ambiente <c>Jwt__Secret</c> — nunca versionado no appsettings.
    /// </remarks>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Validade do access token, em minutos.
    /// </summary>
    /// <remarks>
    /// Curta de proposito: um access token vazado tem janela de abuso pequena. A
    /// renovacao continua transparente para o cliente gracas ao refresh token.
    /// </remarks>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Validade do refresh token, em dias.
    /// </summary>
    /// <remarks>
    /// Longa para nao forcar login constante, mas mitigada por rotacao: cada uso do
    /// refresh token revoga o anterior e emite um novo.
    /// </remarks>
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
