using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Validates refresh token requests.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// Initializes the validator rules for <see cref="RefreshTokenCommand"/>.
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}