using Inventory.Application.Persistence;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using Marketplace.SharedKernel.Exceptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Consumers;

/// <summary>
/// Reserva o estoque dos itens de um pedido cujo pagamento foi aprovado.
/// </summary>
/// <remarks>
/// <para>
/// <b>Este consumidor concentra a correcao mais importante do projeto.</b> A versao
/// anterior fazia literalmente isto:
/// </para>
/// <code>
/// var stockItem = await dbContext.StockItems
///     .OrderBy(item => item.ProductId)
///     .FirstOrDefaultAsync(ct);      // pega o PRIMEIRO item da tabela inteira
/// stockItem.QuantityAvailable -= 1;  // e sempre 1 unidade
/// </code>
/// <para>
/// Ou seja: comprar 3 teclados dava baixa em 1 unidade de um produto qualquer. O codigo
/// compilava, os testes (que nao existiam) passariam, e o estoque divergiria da
/// realidade em toda venda.
/// </para>
/// <para>
/// A raiz do problema era de contrato, nao de codigo: o <c>PaymentApprovedEvent</c> nao
/// carregava os itens, entao <i>nao havia como</i> saber o que reservar. Corrigir exigiu
/// mudar o evento — bom lembrete de que um modelo de mensagem incompleto empurra o
/// consumidor para a gambiarra.
/// </para>
/// <para>
/// <b>Ordem das operacoes.</b> A deduplicacao vem primeiro: reservar estoque duas vezes
/// pelo mesmo pedido tiraria unidades reais do deposito sem venda correspondente.
/// </para>
/// </remarks>
/// <param name="inventoryRepository">Repositorio de estoque.</param>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class PaymentApprovedConsumer(
    IInventoryRepository inventoryRepository,
    RedisMessageDeduplicator deduplicator,
    ILogger<PaymentApprovedConsumer> logger) : IConsumer<PaymentApprovedEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PaymentApprovedEvent> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.EventId;

        if (!await deduplicator.TryBeginAsync(messageId, GetType().FullName!, context.CancellationToken))
        {
            return;
        }

        var requestedQuantities = message.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        try
        {
            await inventoryRepository.ReserveAsync(requestedQuantities, context.CancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            // Falta de estoque e um resultado de negocio legitimo, nao uma falha tecnica.
            // Por isso o fluxo continua publicando o evento de compensacao, em vez de
            // deixar a excecao subir e mandar a mensagem para a fila de erro.
            logger.LogWarning(
                "Reserva de estoque do pedido {OrderId} nao pode ser concluida: {Reason}",
                message.OrderId,
                exception.Message);

            await context.Publish(
                new StockReservationFailedEvent(
                    Guid.NewGuid(),
                    message.OrderId,
                    message.UserId,
                    exception.Message,
                    DateTime.UtcNow),
                context.CancellationToken);

            return;
        }

        logger.LogInformation(
            "Estoque reservado para o pedido {OrderId} ({ItemCount} produtos).",
            message.OrderId,
            requestedQuantities.Count);

        await context.Publish(
            new StockReservedEvent(Guid.NewGuid(), message.OrderId, message.UserId, DateTime.UtcNow),
            context.CancellationToken);
    }
}
