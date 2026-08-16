namespace Identity.Domain.Entities;

/// <summary>
/// Endereco cadastrado por um usuario.
/// </summary>
/// <remarks>
/// <para>
/// Aqui o endereco e uma <b>entidade</b> (tem <see cref="Id"/> proprio, pois o usuario
/// precisa escolher "entregar neste") — enquanto dentro do pedido o mesmo conceito vira
/// um <b>objeto de valor</b> (<c>AddressSnapshot</c>), congelado e sem identidade.
/// </para>
/// <para>
/// O mesmo conceito de negocio modelado de duas formas diferentes conforme o contexto
/// e exatamente o que DDD chama de <i>bounded context</i>: nao existe "a" classe
/// Endereco universal, existe a que faz sentido para cada limite.
/// </para>
/// </remarks>
public sealed class Address
{
    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private Address()
    {
    }

    /// <summary>
    /// Cria um endereco.
    /// </summary>
    /// <param name="id">Identificador do endereco.</param>
    /// <param name="userId">Usuario dono do endereco.</param>
    /// <param name="street">Logradouro.</param>
    /// <param name="number">Numero.</param>
    /// <param name="city">Cidade.</param>
    /// <param name="state">Estado ou provincia.</param>
    /// <param name="zipCode">CEP.</param>
    /// <param name="country">Pais.</param>
    public Address(Guid id, Guid userId, string street, string number, string city, string state, string zipCode, string country)
    {
        Id = id;
        UserId = userId;
        Street = street.Trim();
        Number = number.Trim();
        City = city.Trim();
        State = state.Trim();
        ZipCode = zipCode.Trim();
        Country = country.Trim();
    }

    /// <summary>
    /// Identificador do endereco.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Usuario dono do endereco.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Logradouro.
    /// </summary>
    public string Street { get; private set; } = string.Empty;

    /// <summary>
    /// Numero.
    /// </summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>
    /// Cidade.
    /// </summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>
    /// Estado ou provincia.
    /// </summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>
    /// CEP.
    /// </summary>
    public string ZipCode { get; private set; } = string.Empty;

    /// <summary>
    /// Pais.
    /// </summary>
    public string Country { get; private set; } = string.Empty;
}
