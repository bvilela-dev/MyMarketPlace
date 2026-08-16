using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

/// <summary>
/// Comando de inclusao de um endereco no perfil de um usuario.
/// </summary>
/// <param name="UserId">Usuario dono do endereco.</param>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record AddAddressCommand(
    Guid UserId,
    string Street,
    string Number,
    string City,
    string State,
    string ZipCode,
    string Country) : IRequest<AddressDto>;
