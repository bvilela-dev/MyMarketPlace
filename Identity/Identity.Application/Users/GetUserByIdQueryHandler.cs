using Identity.Application.Abstractions;
using Identity.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users;

/// <summary>
/// Busca o perfil de um usuario pelo identificador.
/// </summary>
/// <remarks>
/// <para>
/// Consulta de leitura pura, com duas otimizacoes que valem para toda query do projeto:
/// </para>
/// <list type="bullet">
///   <item><c>AsNoTracking()</c> — dispensa o change tracker do EF Core. Como nada sera
///   alterado, rastrear as entidades so gastaria memoria e CPU.</item>
///   <item>projecao direta para DTO com <c>Select</c> — o SQL gerado traz apenas as
///   colunas usadas, em vez de <c>SELECT *</c> seguido de mapeamento em memoria.</item>
/// </list>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Identity.</param>
public sealed class GetUserByIdQueryHandler(IIdentityDbContext dbContext) : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    /// <inheritdoc />
    public Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        => dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == request.UserId)
            .Select(user => new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAtUtc,
                user.Addresses
                    .Select(address => new AddressDto(
                        address.Id,
                        address.Street,
                        address.Number,
                        address.City,
                        address.State,
                        address.ZipCode,
                        address.Country))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
