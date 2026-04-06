using FluentValidation;

namespace Identity.Application.Users;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
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