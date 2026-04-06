using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Users;

/// <summary>
/// Represents the request to append a new address to a user.
/// </summary>
/// <param name="UserId">The target user identifier.</param>
/// <param name="Street">The street name.</param>
/// <param name="Number">The street number.</param>
/// <param name="City">The city name.</param>
/// <param name="State">The state or province.</param>
/// <param name="ZipCode">The postal code.</param>
/// <param name="Country">The country name.</param>
public sealed record AddAddressCommand(Guid UserId, string Street, string Number, string City, string State, string ZipCode, string Country) : IRequest<AddressDto>;