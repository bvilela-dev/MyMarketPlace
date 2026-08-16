using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Regras de validacao do cadastro de usuario.
/// </summary>
/// <remarks>
/// Roda no <c>ValidationBehavior</c>, antes do handler. Toda falha aqui vira HTTP 400
/// com a lista de campos invalidos, sem tocar no banco.
/// </remarks>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Define as regras de <see cref="RegisterUserCommand"/>.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("O nome e obrigatorio.")
            .MaximumLength(120).WithMessage("O nome deve ter no maximo 120 caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("O e-mail e obrigatorio.")
            .EmailAddress().WithMessage("Informe um e-mail valido.")
            .MaximumLength(256).WithMessage("O e-mail deve ter no maximo 256 caracteres.");

        // 8 caracteres e o minimo pratico recomendado pelo NIST SP 800-63B. A norma
        // desaconselha exigir simbolo/maiuscula obrigatorios: complexidade forcada leva
        // o usuario a padroes previsiveis ("Senha@123") e a anotar a senha.
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("A senha e obrigatoria.")
            .MinimumLength(8).WithMessage("A senha deve ter ao menos 8 caracteres.")
            // BCrypt trunca silenciosamente a partir de 72 bytes: sem este limite, o
            // final de uma senha muito longa seria ignorado sem qualquer aviso.
            .MaximumLength(72).WithMessage("A senha deve ter no maximo 72 caracteres.");
    }
}
