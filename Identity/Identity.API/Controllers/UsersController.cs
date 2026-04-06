using Identity.Application.Users;
using Identity.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Exposes user profile endpoints.
/// </summary>
/// <param name="sender">Mediator used to dispatch user commands and queries.</param>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves a user profile by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The user profile when found; otherwise, <see cref="NotFoundResult"/>.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>
    /// Adds a new address to the specified user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="request">The address payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created address data.</returns>
    [HttpPost("{id:guid}/addresses")]
    public Task<AddressDto> AddAddress(Guid id, [FromBody] AddAddressRequest request, CancellationToken cancellationToken)
        => sender.Send(new AddAddressCommand(id, request.Street, request.Number, request.City, request.State, request.ZipCode, request.Country), cancellationToken);
}

/// <summary>
/// Represents the HTTP payload used to add a user address.
/// </summary>
/// <param name="Street">The street name.</param>
/// <param name="Number">The street number.</param>
/// <param name="City">The city name.</param>
/// <param name="State">The state or province.</param>
/// <param name="ZipCode">The postal code.</param>
/// <param name="Country">The country name.</param>
public sealed record AddAddressRequest(string Street, string Number, string City, string State, string ZipCode, string Country);