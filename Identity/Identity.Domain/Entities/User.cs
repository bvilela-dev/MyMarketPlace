using Identity.Domain.Events;
using Marketplace.SharedKernel.Abstractions;

namespace Identity.Domain.Entities;

/// <summary>
/// Agregado que representa um usuario do marketplace.
/// </summary>
/// <remarks>
/// <para>
/// <b>Raiz do agregado</b> formado por <see cref="Address"/> e <see cref="RefreshToken"/>.
/// Nenhum dos dois e manipulado diretamente: enderecos e tokens sao criados pelos
/// metodos <see cref="AddAddress"/> e <see cref="AddRefreshToken"/>, que sao os unicos
/// pontos onde as invariantes podem ser garantidas.
/// </para>
/// <para>
/// <b>Por que as colecoes sao expostas como <c>IReadOnlyCollection</c>?</b> Se
/// <c>Addresses</c> fosse um <c>List</c> publico, qualquer codigo poderia chamar
/// <c>user.Addresses.Add(...)</c> pulando as regras da entidade. O encapsulamento das
/// colecoes e o que impede o modelo anemico.
/// </para>
/// </remarks>
public sealed class User : AggregateRoot
{
    private readonly List<Address> _addresses = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private User()
    {
    }

    /// <summary>
    /// Cria um novo usuario.
    /// </summary>
    /// <param name="id">Identificador do usuario.</param>
    /// <param name="name">Nome de exibicao.</param>
    /// <param name="email">E-mail ja normalizado (minusculo e sem espacos).</param>
    /// <param name="passwordHash">Hash BCrypt da senha — nunca a senha em texto puro.</param>
    /// <exception cref="ArgumentException">
    /// Lancada quando nome, e-mail ou hash chegam vazios. Guardas no construtor garantem
    /// que nao existe instancia invalida de <see cref="User"/> em memoria, mesmo que
    /// alguem esqueca de validar na camada de aplicacao.
    /// </exception>
    public User(Guid id, string name, string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Id = id;
        Name = name.Trim();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTime.UtcNow;

        Raise(new UserCreatedDomainEvent(id, Name, Email, CreatedAtUtc));
    }

    /// <summary>
    /// Nome de exibicao do usuario.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// E-mail normalizado, unico no sistema.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt da senha.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Momento (UTC) do cadastro.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Enderecos cadastrados pelo usuario.
    /// </summary>
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    /// <summary>
    /// Refresh tokens ja emitidos para o usuario.
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    /// Normaliza um e-mail para comparacao e armazenamento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Este metodo corrige um bug real que existia no projeto.</b> O cadastro
    /// verificava a duplicidade com o e-mail <i>cru</i> digitado, mas gravava a versao
    /// normalizada. Resultado: cadastrar <c>"Ana@Teste.com"</c> depois de
    /// <c>"ana@teste.com"</c> passava pela checagem e so estourava la no banco, como
    /// violacao de indice unico — devolvendo HTTP 500 em vez de uma mensagem clara.
    /// </para>
    /// <para>
    /// Centralizar a normalizacao na entidade garante que cadastro e login apliquem
    /// exatamente a mesma regra, para sempre.
    /// </para>
    /// <para>
    /// Usa-se <c>ToLowerInvariant</c> (e nao <c>ToLower</c>) para nao depender da cultura
    /// do servidor. O caso famoso e o turco, onde 'I' minusculo vira 'ı' (sem ponto) e
    /// dois e-mails identicos deixariam de bater conforme o locale do container.
    /// </para>
    /// </remarks>
    /// <param name="email">E-mail informado pelo usuario.</param>
    /// <returns>E-mail em minusculo e sem espacos nas pontas.</returns>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Adiciona um endereco ao usuario.
    /// </summary>
    /// <param name="street">Logradouro.</param>
    /// <param name="number">Numero.</param>
    /// <param name="city">Cidade.</param>
    /// <param name="state">Estado ou provincia.</param>
    /// <param name="zipCode">CEP.</param>
    /// <param name="country">Pais.</param>
    /// <returns>O endereco criado.</returns>
    public Address AddAddress(string street, string number, string city, string state, string zipCode, string country)
    {
        var address = new Address(Guid.NewGuid(), Id, street, number, city, state, zipCode, country);
        _addresses.Add(address);
        return address;
    }

    /// <summary>
    /// Registra um novo refresh token para o usuario.
    /// </summary>
    /// <param name="tokenHash">Hash SHA-256 (Base64) do token entregue ao cliente.</param>
    /// <param name="expiresAtUtc">Momento (UTC) de expiracao.</param>
    /// <returns>O refresh token criado.</returns>
    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAtUtc)
    {
        var refreshToken = new RefreshToken(Guid.NewGuid(), Id, tokenHash, expiresAtUtc);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    /// <summary>
    /// Revoga todos os refresh tokens ativos do usuario.
    /// </summary>
    /// <remarks>
    /// Suporta o cenario "sair de todos os dispositivos" e a reacao a suspeita de
    /// vazamento de token.
    /// </remarks>
    /// <param name="utcNow">Instante atual em UTC.</param>
    public void RevokeAllRefreshTokens(DateTime utcNow)
    {
        foreach (var token in _refreshTokens.Where(token => token.IsActive(utcNow)))
        {
            token.Revoke();
        }
    }
}
