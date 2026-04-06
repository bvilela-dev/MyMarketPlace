using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Validates user registration requests.
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Initializes the validator rules for <see cref="RegisterUserCommand"/>.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
    }
}