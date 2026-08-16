using FluentValidation;

namespace Catalog.Application.Products;

/// <summary>
/// Regras de validacao do cadastro de produto.
/// </summary>
/// <remarks>
/// <para>
/// As mesmas regras de preco e quantidade tambem existem no construtor de
/// <c>Product</c>. A duplicacao e intencional e cada camada tem um papel:
/// </para>
/// <list type="bullet">
///   <item><b>Validador</b>: barra cedo, devolve HTTP 400 com todos os campos invalidos
///   de uma vez e nao toca no banco.</item>
///   <item><b>Dominio</b>: e a garantia final. Vale para qualquer caminho de entrada —
///   um consumidor de fila, um job de importacao, um teste — nao apenas para o HTTP.</item>
/// </list>
/// </remarks>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Define as regras de <see cref="CreateProductCommand"/>.
    /// </summary>
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("O nome do produto e obrigatorio.")
            .MaximumLength(120).WithMessage("O nome deve ter no maximo 120 caracteres.");

        RuleFor(command => command.Description)
            .MaximumLength(1024).WithMessage("A descricao deve ter no maximo 1024 caracteres.");

        RuleFor(command => command.Price)
            .GreaterThan(0).WithMessage("O preco deve ser maior que zero.")
            .LessThanOrEqualTo(1_000_000m).WithMessage("O preco excede o limite permitido.");

        RuleFor(command => command.AvailableQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade nao pode ser negativa.");
    }
}
