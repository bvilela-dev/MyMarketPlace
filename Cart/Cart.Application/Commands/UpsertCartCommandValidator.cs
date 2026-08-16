using FluentValidation;

namespace Cart.Application.Commands;

/// <summary>
/// Regras de validacao da gravacao de carrinho.
/// </summary>
/// <remarks>
/// O Cart nao consulta o Catalog para conferir preco: o carrinho e uma area de rascunho
/// do cliente. O preco que vale e o buscado pelo Order no momento da compra — por isso
/// um preco incorreto aqui nao tem consequencia financeira.
/// </remarks>
public sealed class UpsertCartCommandValidator : AbstractValidator<UpsertCartCommand>
{
    /// <summary>
    /// Define as regras de <see cref="UpsertCartCommand"/>.
    /// </summary>
    public UpsertCartCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();

        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(line => line.ProductId).NotEmpty().WithMessage("O produto e obrigatorio.");
            item.RuleFor(line => line.Name).NotEmpty().MaximumLength(256);
            item.RuleFor(line => line.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("O preco nao pode ser negativo.");
            item.RuleFor(line => line.Quantity)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.")
                .LessThanOrEqualTo(1_000).WithMessage("A quantidade por item nao pode passar de 1.000.");
        });
    }
}
