using Inventory.Application.Persistence;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Consumers;

/// <summary>
/// Cria a linha de estoque de um produto recem-cadastrado no catalogo.
/// </summary>
/// <remarks>
/// <para>
/// Este consumidor e o que mantem Catalog e Inventory alinhados sem que nenhum dos dois
/// leia o banco do outro. O Catalog anuncia "produto criado"; o Inventory reage abrindo
/// o saldo inicial.
/// </para>
/// <para>
/// <b>Alternativa descartada:</b> semear os dois bancos com a mesma lista de GUIDs. Alem
/// de fragil (duas listas para manter iguais a mao), so funcionaria para os dados de
/// demonstracao — produtos cadastrados pela API ficariam sem estoque.
/// </para>
/// </remarks>
/// <param name="inventoryRepository">Repositorio de estoque.</param>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class ProductCreatedConsumer(
    IInventoryRepository inventoryRepository,
    RedisMessageDeduplicator deduplicator,
    ILogger<ProductCreatedConsumer> logger) : IConsumer<ProductCreatedEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;

        if (!await deduplicator.TryBeginAsync(messageId, GetType().FullName!, context.CancellationToken))
        {
            return;
        }

        // O repositorio faz upsert: se a linha ja existir (reprocessamento apos a janela
        // de deduplicacao expirar), o saldo NAO e somado de novo. Idempotencia em duas
        // camadas — a marca no Redis e a propria operacao ser segura de repetir.
        await inventoryRepository.EnsureStockAsync(message.ProductId, message.InitialQuantity, context.CancellationToken);

        logger.LogInformation(
            "Estoque inicial de {Quantity} unidades criado para o produto {ProductId} ({Name}).",
            message.InitialQuantity,
            message.ProductId,
            message.Name);
    }
}
