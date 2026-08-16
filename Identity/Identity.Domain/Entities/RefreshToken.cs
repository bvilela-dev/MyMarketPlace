namespace Identity.Domain.Entities;

/// <summary>
/// Refresh token emitido para um usuario.
/// </summary>
/// <remarks>
/// <para>
/// <b>O que muda em relacao ao access token.</b> O access token e um JWT auto-contido e
/// de vida curta: qualquer servico valida a assinatura sem consultar banco. O refresh
/// token e o oposto — opaco, de vida longa e <i>com estado</i>, porque precisa poder ser
/// revogado. E justamente esse estado que permite "deslogar de todos os dispositivos".
/// </para>
/// <para>
/// <b>Somente o hash e persistido.</b> A entidade guarda <see cref="TokenHash"/>, nunca
/// o valor original. Se o banco vazar, os tokens roubados nao servem para nada — mesma
/// logica de nunca armazenar senha em texto puro. Como o token e gerado com 64 bytes
/// aleatorios, SHA-256 puro basta: nao ha espaco de busca para forca bruta, entao nao e
/// preciso o custo do BCrypt aqui.
/// </para>
/// <para>
/// <b>Rotacao.</b> Cada uso do refresh token o revoga e emite um novo. Se um token ja
/// usado reaparecer, e sinal forte de vazamento — o gancho para reagir a isso e o
/// campo <see cref="IsRevoked"/>.
/// </para>
/// </remarks>
public sealed class RefreshToken
{
    /// <summary>
    /// Construtor sem parametros exigido pelo EF Core para materializar a entidade.
    /// </summary>
    /// <remarks>
    /// Privado de proposito: o EF acessa por reflexao, mas o codigo da aplicacao fica
    /// obrigado a usar o construtor publico, que garante o preenchimento dos campos.
    /// </remarks>
    private RefreshToken()
    {
    }

    /// <summary>
    /// Cria um refresh token.
    /// </summary>
    /// <param name="id">Identificador do token.</param>
    /// <param name="userId">Usuario dono do token.</param>
    /// <param name="tokenHash">Hash SHA-256 (Base64) do valor entregue ao cliente.</param>
    /// <param name="expiresAtUtc">Momento (UTC) de expiracao.</param>
    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Identificador do token.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Usuario dono do token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Hash SHA-256 (Base64) do token. O valor original existe apenas com o cliente.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Momento (UTC) de emissao.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Momento (UTC) de expiracao.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Indica que o token foi revogado e nao pode mais ser trocado.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Indica se o token ainda pode ser usado (nao revogado e dentro da validade).
    /// </summary>
    /// <remarks>
    /// Regra de negocio dentro da entidade, e nao espalhada pelos handlers: assim
    /// nenhum caso de uso pode "esquecer" de checar a expiracao.
    /// </remarks>
    /// <param name="utcNow">Instante atual em UTC (injetado para permitir teste deterministico).</param>
    /// <returns><see langword="true"/> quando o token e utilizavel.</returns>
    public bool IsActive(DateTime utcNow) => !IsRevoked && ExpiresAtUtc > utcNow;

    /// <summary>
    /// Revoga o token.
    /// </summary>
    public void Revoke() => IsRevoked = true;
}
