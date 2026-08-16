using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Regras de validacao do login.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Define as regras de <see cref="LoginCommand"/>.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("O e-mail e obrigatorio.")
            .EmailAddress().WithMessage("Informe um e-mail valido.");

        // Nao se valida tamanho minimo de senha no login: a regra pode ter mudado desde
        // o cadastro, e rejeitar aqui bloquearia usuarios antigos legitimos.
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("A senha e obrigatoria.");
    }
}
