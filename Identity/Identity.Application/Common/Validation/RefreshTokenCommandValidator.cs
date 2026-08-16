using FluentValidation;

namespace Identity.Application.Auth;

/// <summary>
/// Regras de validacao da renovacao de token.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// Define as regras de <see cref="RefreshTokenCommand"/>.
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("O refresh token e obrigatorio.");
    }
}
