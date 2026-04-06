using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using MediatR;

namespace Cart.Application.Queries;

public sealed class GetCartQueryHandler(ICartStore cartStore) : IRequestHandler<GetCartQuery, ShoppingCart?>
{
    public Task<ShoppingCart?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        => cartStore.GetAsync(request.UserId, cancellationToken);
}