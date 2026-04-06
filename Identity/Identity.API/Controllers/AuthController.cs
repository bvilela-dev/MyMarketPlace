using Identity.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Exposes authentication endpoints for registration, login, and token refresh.
/// </summary>
/// <param name="sender">Mediator used to dispatch authentication commands.</param>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Registers a new user account and returns the initial token pair.
    /// </summary>
    /// <param name="command">The registration payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authentication response for the created user.</returns>
    [HttpPost("register")]
    public Task<AuthResponse> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    /// <summary>
    /// Authenticates an existing user and returns a new token pair.
    /// </summary>
    /// <param name="command">The login payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authentication response for the authenticated user.</returns>
    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    /// <summary>
    /// Exchanges a refresh token for a new access token and refresh token pair.
    /// </summary>
    /// <param name="command">The refresh token payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The refreshed authentication response.</returns>
    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);
}