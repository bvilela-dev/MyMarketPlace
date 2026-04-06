using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Validates login requests.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes the validator rules for <see cref="LoginCommand"/>.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}