using Identity.Application.Abstractions;
using Identity.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Users;

public sealed class AddAddressCommandHandler(IIdentityDbContext dbContext) : IRequestHandler<AddAddressCommand, AddressDto>
{
    public async Task<AddressDto> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.Addresses)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var address = user.AddAddress(request.Street, request.Number, request.City, request.State, request.ZipCode, request.Country);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddressDto(address.Id, address.Street, address.Number, address.City, address.State, address.ZipCode, address.Country);
    }
}