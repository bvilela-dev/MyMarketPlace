using Identity.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public Task<AuthResponse> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);
}