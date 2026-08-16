using System.Text.Json;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using Marketplace.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;

namespace Order.Application.Orders;

/// <summary>
/// Cria um pedido apos validar o endereco e a disponibilidade dos produtos.
/// </summary>
/// <remarks>
/// <para>
/// Caso de uso mais completo do projeto: combina duas chamadas gRPC sincronas com uma
/// publicacao assincrona via outbox.
/// </para>
/// <code>
/// 1. valida usuario + endereco ............ gRPC -> Identity   (sincrono: bloqueia a decisao)
/// 2. busca preco e estoque de cada item ... gRPC -> Catalog    (sincrono: precisa do preco agora)
/// 3. monta o agregado Order ............... dominio            (calcula o total)
/// 4. grava pedido + OrderCreatedEvent ..... 1 transacao        (outbox)
/// 5. responde 201 ......................... o resto segue por eventos
/// </code>
/// <para>
/// <b>Por que buscar o preco no Catalog em vez de aceitar o do cliente?</b> Porque preco
/// enviado pelo cliente e preco escolhido pelo cliente. Esta e a falha classica de
/// e-commerce: aceitar <c>unitPrice</c> no payload permite comprar um monitor por
/// R$ 0,01. O preco vem sempre do servidor.
/// </para>
/// <para>
/// <b>Limitacao conhecida (documentada de proposito):</b> a verificacao de estoque aqui
/// e apenas uma triagem rapida contra o numero da vitrine. A reserva real acontece no
/// Inventory, depois do pagamento — e e la que existe a garantia transacional. Entre a
/// consulta e a reserva o estoque pode acabar, e por isso existe o
/// <c>StockReservationFailedEvent</c> e o estado <c>Cancelled</c>.
/// </para>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Order.</param>
/// <param name="catalogGrpcClient">Cliente gRPC do Catalog.</param>
/// <param name="identityGrpcClient">Cliente gRPC do Identity.</param>
/// <param name="logger">Logger do caso de uso.</param>
public sealed class CreateOrderCommandHandler(
    IOrderDbContext dbContext,
    ICatalogGrpcClient catalogGrpcClient,
    IIdentityGrpcClient identityGrpcClient,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    /// <summary>
    /// Moeda usada pelo marketplace nesta versao.
    /// </summary>
    private const string Currency = "BRL";

    /// <inheritdoc />
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var validatedAddress = await identityGrpcClient.ValidateUserAddressAsync(request.UserId, request.AddressId, cancellationToken);

        if (!validatedAddress.IsValid)
        {
            // Mensagem generica de proposito: dizer "endereco existe mas e de outro
            // usuario" permitiria descobrir ids validos por tentativa e erro.
            throw new BusinessRuleException("Usuario ou endereco de entrega invalido.");
        }

        var orderItems = await BuildOrderItemsAsync(request, cancellationToken);

        var addressSnapshot = new AddressSnapshot(
            validatedAddress.Street,
            validatedAddress.Number,
            validatedAddress.City,
            validatedAddress.State,
            validatedAddress.ZipCode,
            validatedAddress.Country);

        var order = new Order.Domain.Entities.Order(Guid.NewGuid(), request.UserId, addressSnapshot, orderItems);

        await dbContext.Orders.AddAsync(order, cancellationToken);

        await dbContext.OutboxMessages.AddAsync(
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(OrderCreatedEvent).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new OrderCreatedEvent(
                    Guid.NewGuid(),
                    order.Id,
                    order.UserId,
                    order.Total,
                    Currency,
                    new AddressSnapshotDto(
                        addressSnapshot.Street,
                        addressSnapshot.Number,
                        addressSnapshot.City,
                        addressSnapshot.State,
                        addressSnapshot.ZipCode,
                        addressSnapshot.Country),
                    order.Items
                        // Qualificado por completo: existe tambem um OrderItemDto na
                        // camada de aplicacao (o da API REST). Sao contratos diferentes
                        // com o mesmo nome — este e o que trafega no barramento.
                        .Select(item => new Marketplace.Contracts.Events.OrderItemDto(
                            item.ProductId,
                            item.Name,
                            item.UnitPrice,
                            item.Quantity))
                        .ToArray(),
                    order.CreatedAtUtc)),
                OccurredOnUtc = DateTime.UtcNow
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Pedido {OrderId} criado para o usuario {UserId} no valor de {Total}.",
            order.Id,
            order.UserId,
            order.Total);

        return new CreateOrderResponse(order.Id, order.UserId, order.Total, Currency, order.Status.ToString(), order.CreatedAtUtc);
    }

    /// <summary>
    /// Consulta o Catalog e converte cada linha solicitada num item de pedido.
    /// </summary>
    /// <remarks>
    /// <para>
    /// As consultas rodam <b>em paralelo</b> com <c>Task.WhenAll</c>. Um pedido com 10
    /// itens, em serie, somaria 10 idas e voltas de rede; em paralelo, o custo e o da
    /// chamada mais lenta. Como o Catalog e apenas consultado (sem escrita), nao ha
    /// risco de ordem ou de concorrencia.
    /// </para>
    /// <para>
    /// Itens repetidos do mesmo produto sao agrupados antes da consulta: evita perguntar
    /// duas vezes pelo mesmo produto e impede burlar a checagem de estoque enviando
    /// varias linhas pequenas do mesmo item.
    /// </para>
    /// </remarks>
    private async Task<List<OrderItem>> BuildOrderItemsAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var requestedQuantities = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var products = await Task.WhenAll(requestedQuantities.Keys
            .Select(productId => catalogGrpcClient.GetProductAsync(productId, cancellationToken)));

        var orderItems = new List<OrderItem>(products.Length);

        foreach (var product in products)
        {
            var quantity = requestedQuantities[product.ProductId];

            if (product.AvailableQuantity < quantity)
            {
                throw new BusinessRuleException(
                    $"Estoque insuficiente para o produto '{product.Name}': disponivel {product.AvailableQuantity}, solicitado {quantity}.");
            }

            orderItems.Add(new OrderItem(product.ProductId, product.Name, product.Price, quantity));
        }

        return orderItems;
    }
}
