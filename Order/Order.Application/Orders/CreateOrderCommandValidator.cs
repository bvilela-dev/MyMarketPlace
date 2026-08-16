using FluentValidation;

namespace Order.Application.Orders;

/// <summary>
/// Regras de validacao da criacao de pedido.
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Quantidade maxima de linhas distintas por pedido.
    /// </summary>
    /// <remarks>
    /// Cada linha vira uma chamada gRPC ao Catalog. Sem limite, um pedido com 10.000
    /// itens viraria um vetor de negacao de servico contra o proprio catalogo.
    /// </remarks>
    private const int MaxItems = 100;

    /// <summary>
    /// Define as regras de <see cref="CreateOrderCommand"/>.
    /// </summary>
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("Usuario nao identificado.");

        RuleFor(command => command.AddressId)
            .NotEmpty().WithMessage("O endereco de entrega e obrigatorio.");

        RuleFor(command => command.Items)
            .NotEmpty().WithMessage("O pedido precisa ter ao menos um item.")
            .Must(items => items.Count <= MaxItems)
            .WithMessage($"O pedido pode ter no maximo {MaxItems} itens distintos.");

        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(line => line.ProductId)
                .NotEmpty().WithMessage("O produto e obrigatorio.");

            item.RuleFor(line => line.Quantity)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.")
                .LessThanOrEqualTo(1_000).WithMessage("A quantidade por item nao pode passar de 1.000.");
        });
    }
}
