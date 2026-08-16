using FluentValidation;

namespace Identity.Application.Users;

/// <summary>
/// Regras de validacao do cadastro de endereco.
/// </summary>
public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    /// <summary>
    /// Define as regras de <see cref="AddAddressCommand"/>.
    /// </summary>
    /// <remarks>
    /// Os limites de tamanho espelham exatamente os <c>HasMaxLength</c> do
    /// <c>AddressConfiguration</c>. Manter os dois alinhados transforma o que seria um
    /// erro de truncamento do Postgres (HTTP 500) numa mensagem clara de campo invalido
    /// (HTTP 400).
    /// </remarks>
    public AddAddressCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Street).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Number).NotEmpty().MaximumLength(32);
        RuleFor(command => command.City).NotEmpty().MaximumLength(120);
        RuleFor(command => command.State).NotEmpty().MaximumLength(120);
        RuleFor(command => command.ZipCode).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(120);
    }
}
