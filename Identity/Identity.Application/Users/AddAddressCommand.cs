using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

public sealed record AddAddressCommand(Guid UserId, string Street, string Number, string City, string State, string ZipCode, string Country) : IRequest<AddressDto>;