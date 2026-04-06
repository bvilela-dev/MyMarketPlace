using FluentValidation;

namespace Order.Application.Orders;

/// <summary>
/// Validates order creation requests.
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Initializes the validator rules for <see cref="CreateOrderCommand"/>.
    /// </summary>
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.AddressId).NotEmpty();
        RuleFor(command => command.Items).NotEmpty();
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}