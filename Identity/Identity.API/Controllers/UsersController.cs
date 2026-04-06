using Identity.Application.Users;
using Identity.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/addresses")]
    public Task<AddressDto> AddAddress(Guid id, [FromBody] AddAddressRequest request, CancellationToken cancellationToken)
        => sender.Send(new AddAddressCommand(id, request.Street, request.Number, request.City, request.State, request.ZipCode, request.Country), cancellationToken);
}

public sealed record AddAddressRequest(string Street, string Number, string City, string State, string ZipCode, string Country);