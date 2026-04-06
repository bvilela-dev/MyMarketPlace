using FluentValidation;

namespace Identity.Application.Users;

/// <summary>
/// Validates address creation requests.
/// </summary>
public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    /// <summary>
    /// Initializes the validator rules for <see cref="AddAddressCommand"/>.
    /// </summary>
    public AddAddressCommandValidator()
    {
        RuleFor(command => command.Street).NotEmpty();
        RuleFor(command => command.Number).NotEmpty();
        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.State).NotEmpty();
        RuleFor(command => command.ZipCode).NotEmpty();
        RuleFor(command => command.Country).NotEmpty();
    }
}