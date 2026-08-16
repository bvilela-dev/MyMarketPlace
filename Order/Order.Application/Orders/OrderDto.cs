namespace Order.Application.Orders;

/// <summary>
/// Pedido exposto pela API.
/// </summary>
/// <param name="Id">Identificador do pedido.</param>
/// <param name="UserId">Usuario dono do pedido.</param>
/// <param name="Total">Valor total.</param>
/// <param name="Status">
/// Estado atual: <c>PendingPayment</c>, <c>Paid</c>, <c>Confirmed</c>,
/// <c>PaymentFailed</c> ou <c>Cancelled</c>.
/// </param>
/// <param name="StatusReason">Motivo da recusa ou do cancelamento, quando houver.</param>
/// <param name="CreatedAtUtc">Momento (UTC) da criacao.</param>
/// <param name="UpdatedAtUtc">Momento (UTC) da ultima mudanca de estado.</param>
/// <param name="ShippingAddress">Endereco de entrega congelado.</param>
/// <param name="Items">Itens do pedido.</param>
public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    decimal Total,
    string Status,
    string? StatusReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    OrderAddressDto ShippingAddress,
    IReadOnlyCollection<OrderItemDto> Items);

/// <summary>
/// Endereco de entrega do pedido.
/// </summary>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record OrderAddressDto(string Street, string Number, string City, string State, string ZipCode, string Country);

/// <summary>
/// Item do pedido exposto pela API.
/// </summary>
/// <param name="ProductId">Produto comprado.</param>
/// <param name="Name">Nome do produto no momento da compra.</param>
/// <param name="UnitPrice">Preco unitario praticado.</param>
/// <param name="Quantity">Quantidade.</param>
/// <param name="LineTotal">Total da linha.</param>
public sealed record OrderItemDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);
