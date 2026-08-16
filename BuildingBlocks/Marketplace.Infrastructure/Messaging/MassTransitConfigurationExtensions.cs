using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure.Messaging;

/// <summary>
/// Configuracao compartilhada do barramento MassTransit/RabbitMQ.
/// </summary>
public static class MassTransitConfigurationExtensions
{
    /// <summary>
    /// Configura transporte, politicas de resiliencia e convencao de nomes das filas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>As tres camadas de protecao configuradas aqui, e por que a ordem importa:</b>
    /// </para>
    /// <list type="number">
    ///   <item><b>Retry exponencial</b> (mais interno): 3 tentativas com intervalo
    ///   crescente de 1s ate 15s. Cobre a falha transitoria classica — deadlock,
    ///   timeout momentaneo, reeleicao de lider no banco.</item>
    ///   <item><b>Circuit breaker</b>: se mais de 15% das mensagens falharem numa
    ///   janela de 1 minuto (com no minimo 10 mensagens), o circuito abre e para de
    ///   consumir por 1 minuto. Isso protege um servico dependente que ja esta caindo:
    ///   sem ele, o retry viraria um ataque de negacao de servico contra o proprio
    ///   sistema.</item>
    ///   <item><b>Kill switch</b> (mais externo): desconecta o consumidor
    ///   automaticamente quando a taxa de erro fica critica, e o reconecta sozinho.
    ///   Evita que milhares de mensagens sejam movidas para a fila de erro durante
    ///   uma indisponibilidade prolongada — elas ficam na fila, preservadas.</item>
    /// </list>
    /// <para>
    /// <b>Filas _error e _skipped.</b> O MassTransit cria automaticamente uma fila
    /// <c>{nome}_error</c> para mensagens que esgotaram o retry, e <c>{nome}_skipped</c>
    /// para as que nenhum consumidor reconheceu. Sao as dead-letter queues do sistema:
    /// nada e descartado silenciosamente, e da para reprocessar depois de corrigir o bug.
    /// </para>
    /// <para>
    /// <b>Nome das filas.</b> O formatador converte <c>PaymentApprovedConsumer</c> do
    /// servico <c>order</c> na fila <c>order-payment-approved</c>. O prefixo por
    /// servico e obrigatorio e nao e cosmetico — ver o comentario no corpo do metodo.
    /// </para>
    /// </remarks>
    /// <param name="configurator">Configurador do barramento.</param>
    /// <param name="configuration">Fonte de configuracao (le a connection string <c>RabbitMq</c>).</param>
    /// <param name="serviceName">
    /// Prefixo das filas deste servico (ex.: <c>"order"</c>). <b>Obrigatorio</b> — ver a
    /// nota sobre nomes de fila abaixo.
    /// </param>
    public static void ConfigureMarketplaceBus(
        this IBusRegistrationConfigurator configurator,
        IConfiguration configuration,
        string serviceName)
    {
        // ---------------------------------------------------------------------
        // BUG REAL CORRIGIDO AQUI — vale entender em detalhe.
        //
        // Antes: SetKebabCaseEndpointNameFormatter() sem prefixo. O nome da fila saia
        // do nome do consumidor, entao PaymentApprovedConsumer virava a fila
        // "payment-approved" — em TODOS os servicos.
        //
        // Acontece que Order, Inventory e Notification tem, cada um, o seu
        // PaymentApprovedConsumer. Os tres se conectavam a MESMA fila e viravam
        // consumidores concorrentes: o RabbitMQ entrega cada mensagem a UM deles,
        // em rodizio.
        //
        // Efeito pratico: de cada tres pagamentos aprovados, aproximadamente um
        // atualizava o pedido, um reservava estoque e um enviava e-mail — nunca os
        // tres para o mesmo pedido. O sistema "funcionava" e perdia dois tercos do
        // trabalho, sem erro em lugar nenhum.
        //
        // Com o prefixo, cada servico ganha a sua fila e todos recebem uma copia:
        //   order-payment-approved / inventory-payment-approved / notification-payment-approved
        //
        // A regra geral do RabbitMQ: mesma fila = trabalho DIVIDIDO (escala
        // horizontal); filas diferentes ligadas ao mesmo exchange = mensagem
        // COPIADA (publish/subscribe). Replicas do mesmo servico continuam
        // dividindo a mesma fila — que e exatamente o desejado.
        // ---------------------------------------------------------------------
        configurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: serviceName, includeNamespace: false));

        configurator.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "rabbitmq://localhost");

            cfg.UseMessageRetry(retry => retry.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(15),
                intervalDelta: TimeSpan.FromSeconds(2)));

            cfg.UseCircuitBreaker(options =>
            {
                // Numero minimo de mensagens na janela antes de o circuito poder abrir.
                // Sem esse piso, 1 falha em 2 mensagens (50%) abriria o circuito de
                // madrugada, quando o trafego e baixo.
                options.ActiveThreshold = 10;
                options.TrackingPeriod = TimeSpan.FromMinutes(1);
                options.ResetInterval = TimeSpan.FromMinutes(1);
                // Percentual de falha que dispara a abertura do circuito.
                options.TripThreshold = 15;
            });

            cfg.UseKillSwitch(options => options
                .SetActivationThreshold(10)
                .SetTripThreshold(0.5)
                .SetRestartTimeout(TimeSpan.FromMinutes(1)));

            // Descobre e mapeia todos os consumidores registrados no container.
            // Precisa ser a ULTIMA chamada: os filtros configurados acima so se aplicam
            // aos endpoints criados a partir deste ponto.
            cfg.ConfigureEndpoints(context);
        });
    }
}
