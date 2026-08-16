using Marketplace.SharedKernel.Abstractions;

namespace Order.Domain.ValueObjects;

/// <summary>
/// Copia imutavel do endereco de entrega, congelada dentro do pedido.
/// </summary>
/// <remarks>
/// <para>
/// <b>Objeto de valor, nao entidade.</b> Nao tem identidade propria e e comparado pelo
/// conteudo: dois enderecos com os mesmos campos sao o mesmo endereco.
/// </para>
/// <para>
/// <b>Por que copiar em vez de referenciar o <c>AddressId</c> do Identity?</b> Porque o
/// pedido e um registro historico. Se o cliente se mudar amanha, a nota fiscal do pedido
/// de hoje precisa continuar mostrando para onde a mercadoria realmente foi. Guardar um
/// ponteiro reescreveria o passado a cada mudanca de cadastro.
/// </para>
/// <para>
/// Este e tambem o mecanismo que permite ao Order nao depender do Identity para exibir
/// um pedido antigo: o dado necessario ja esta ali.
/// </para>
/// <para>
/// No banco, o EF mapeia com <c>OwnsOne</c> — os campos viram colunas da propria tabela
/// de pedidos, sem chave nem tabela separada.
/// </para>
/// </remarks>
public sealed class AddressSnapshot : ValueObject
{
    /// <summary>
    /// Construtor exigido pelo EF Core.
    /// </summary>
    private AddressSnapshot()
    {
    }

    /// <summary>
    /// Cria a copia do endereco de entrega.
    /// </summary>
    /// <param name="street">Logradouro.</param>
    /// <param name="number">Numero.</param>
    /// <param name="city">Cidade.</param>
    /// <param name="state">Estado ou provincia.</param>
    /// <param name="zipCode">CEP.</param>
    /// <param name="country">Pais.</param>
    public AddressSnapshot(string street, string number, string city, string state, string zipCode, string country)
    {
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

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

    /// <inheritdoc />
    /// <remarks>
    /// A ordem em que os campos sao devolvidos participa do calculo do hash code e
    /// precisa ser estavel — trocar a ordem muda o hash de todos os enderecos.
    /// </remarks>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }
}
