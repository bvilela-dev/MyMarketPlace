namespace Marketplace.Contracts.Events;

/// <summary>
/// Copia imutavel de um endereco, trafegando em eventos de integracao.
/// </summary>
/// <remarks>
/// "Snapshot" e a palavra-chave: o endereco e copiado para dentro do pedido em vez de
/// referenciado por id. Se o cliente se mudar amanha, a nota fiscal do pedido de hoje
/// continua mostrando para onde a mercadoria realmente foi.
/// </remarks>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record AddressSnapshotDto(string Street, string Number, string City, string State, string ZipCode, string Country);
