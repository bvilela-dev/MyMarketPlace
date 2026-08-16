namespace Identity.Application.Models;

/// <summary>
/// Endereco exposto pela API do Identity.
/// </summary>
/// <param name="Id">Identificador do endereco (usado ao criar um pedido).</param>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record AddressDto(Guid Id, string Street, string Number, string City, string State, string ZipCode, string Country);
