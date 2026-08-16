using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Messaging.Idempotency;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Configuration;

namespace Payment.Application.Consumers;

/// <summary>
/// Consome pedidos criados e simula a autorizacao do pagamento.
/// </summary>
/// <remarks>
/// <para>
/// <b>Simulacao, e assumidamente simulacao.</b> Nao existe integracao com adquirente:
/// pedidos ate um teto configuravel sao aprovados, acima disso sao recusados. Isso
/// permite demonstrar os dois caminhos do saga (sucesso e compensacao) sem depender de
/// credenciais externas.
/// </para>
/// <para>
/// O que <b>nao</b> e simulado, e e o que realmente importa mostrar aqui:
/// </para>
/// <list type="bullet">
///   <item><b>Idempotencia</b> — mensagem repetida nao cobra duas vezes;</item>
///   <item><b>propagacao dos itens</b> — o evento de aprovacao carrega as linhas do
///   pedido, sem as quais o Inventory nao teria como saber o que reservar;</item>
///   <item><b>publicacao do fato</b>, e nao envio de comando: o Payment anuncia
///   "pagamento aprovado" e nao sabe quem vai reagir.</item>
/// </list>
/// </remarks>
/// <param name="deduplicator">Deduplicador de mensagens.</param>
/// <param name="options">Configuracao da simulacao de pagamento.</param>
/// <param name="logger">Logger do consumidor.</param>
public sealed class OrderCreatedConsumer(
    RedisMessageDeduplicator deduplicator,
    IOptions<PaymentSimulationOptions> options,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    private readonly PaymentSimulationOptions _options = options.Value;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        // context.MessageId vem do outbox (o Id da linha) e e estavel entre reentregas.
        // O EventId do payload e o plano B, caso a mensagem chegue por outro caminho.
        var messageId = context.MessageId ?? context.Message.EventId;

        if (!await deduplicator.TryBeginAsync(messageId, GetType().FullName!, context.CancellationToken))
        {
            return;
        }

        var order = context.Message;

        // Latencia artificial: torna visivel, na demonstracao, que o pedido fica alguns
        // instantes em PendingPayment antes de virar Paid.
        if (_options.SimulatedLatency > TimeSpan.Zero)
        {
            await Task.Delay(_options.SimulatedLatency, context.CancellationToken);
        }

        if (order.Total > _options.ApprovalLimit)
        {
            var reason = $"Valor de {order.Total:N2} acima do limite simulado de {_options.ApprovalLimit:N2}.";

            logger.LogWarning("Pagamento do pedido {OrderId} recusado: {Reason}", order.OrderId, reason);

            await context.Publish(
                new PaymentFailedEvent(Guid.NewGuid(), order.OrderId, order.UserId, reason, DateTime.UtcNow),
                context.CancellationToken);

            return;
        }

        logger.LogInformation("Pagamento do pedido {OrderId} aprovado no valor de {Total}.", order.OrderId, order.Total);

        await context.Publish(
            new PaymentApprovedEvent(
                Guid.NewGuid(),
                order.OrderId,
                order.UserId,
                order.Total,
                // Repassar os itens e o que permite ao Inventory reservar o produto
                // certo. Sem isso ele so recebia o total — e acabava dando baixa num
                // item arbitrario do estoque.
                order.Items,
                DateTime.UtcNow),
            context.CancellationToken);
    }
}
