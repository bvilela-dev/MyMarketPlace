using Identity.Application.Abstractions;
using Identity.Application.Models;
using Marketplace.SharedKernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users;

/// <summary>
/// Inclui um endereco no perfil de um usuario existente.
/// </summary>
/// <remarks>
/// O <c>Include(Addresses)</c> nao e decorativo: sem carregar a colecao, o EF Core nao
/// consegue rastrear o novo item adicionado pelo metodo <c>User.AddAddress</c> e o
/// endereco simplesmente nao seria gravado.
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Identity.</param>
public sealed class AddAddressCommandHandler(IIdentityDbContext dbContext) : IRequestHandler<AddAddressCommand, AddressDto>
{
    /// <inheritdoc />
    public async Task<AddressDto> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.Addresses)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario", request.UserId);

        var address = user.AddAddress(
            request.Street,
            request.Number,
            request.City,
            request.State,
            request.ZipCode,
            request.Country);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddressDto(
            address.Id,
            address.Street,
            address.Number,
            address.City,
            address.State,
            address.ZipCode,
            address.Country);
    }
}
