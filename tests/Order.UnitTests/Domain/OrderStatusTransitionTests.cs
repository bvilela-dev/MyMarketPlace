using Marketplace.SharedKernel.Exceptions;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;

namespace Order.UnitTests.Domain;

/// <summary>
/// Testes da maquina de estados do pedido — o coracao do saga de checkout.
/// </summary>
/// <remarks>
/// <para>
/// Estes sao os testes mais valiosos do projeto, e por um motivo especifico: a maquina
/// de estados e a unica parte que precisa se comportar corretamente diante de
/// <b>duplicatas</b> e de <b>eventos fora de ordem</b> — dois cenarios que quase nunca
/// aparecem em desenvolvimento e sao rotina em producao.
/// </para>
/// <para>
/// Testes de dominio nao tocam banco, fila ou rede: rodam em milissegundos e nunca
/// falham por motivo alheio a regra que estao verificando.
/// </para>
/// </remarks>
public sealed class OrderStatusTransitionTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Pedido_recem_criado_fica_aguardando_pagamento()
    {
        var order = CreateOrder();

        order.Status.ShouldBe(OrderStatus.PendingPayment);
        order.StatusReason.ShouldBeNull();
    }

    [Fact]
    public void Total_e_a_soma_dos_itens_e_nao_um_valor_informado()
    {
        var order = CreateOrder(
            new OrderItem(Guid.NewGuid(), "Teclado", 349.90m, 2),
            new OrderItem(Guid.NewGuid(), "Mouse", 219.90m, 1));

        // 349,90 x 2 + 219,90 = 919,70
        order.Total.ShouldBe(919.70m);
    }

    [Fact]
    public void Pedido_sem_itens_e_rejeitado()
    {
        var criar = () => new Order.Domain.Entities.Order(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), []);

        criar.ShouldThrow<BusinessRuleException>();
    }

    // ---------------------------------------------------------------- fluxo feliz

    [Fact]
    public void Fluxo_feliz_percorre_pendente_pago_confirmado()
    {
        var order = CreateOrder();

        order.MarkAsPaid(Now).ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Paid);

        order.Confirm(Now.AddSeconds(1)).ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Cada_transicao_atualiza_o_carimbo_de_tempo()
    {
        var order = CreateOrder();
        var depois = Now.AddMinutes(5);

        order.MarkAsPaid(depois);

        order.UpdatedAtUtc.ShouldBe(depois);
        order.UpdatedAtUtc.ShouldBeGreaterThan(order.CreatedAtUtc);
    }

    // ------------------------------------------------------------- idempotencia

    [Fact]
    public void Aprovacao_de_pagamento_repetida_e_ignorada_sem_erro()
    {
        var order = CreateOrder();

        order.MarkAsPaid(Now).ShouldBeTrue();

        // Cenario real: o RabbitMQ reentrega a mesma mensagem porque o ACK se perdeu.
        // O consumidor precisa concluir com sucesso, e nao lancar excecao — caso
        // contrario a mensagem iria para a fila de erro e geraria alarme a toa.
        order.MarkAsPaid(Now.AddSeconds(30)).ShouldBeFalse();

        order.Status.ShouldBe(OrderStatus.Paid);
        // O carimbo de tempo tambem nao muda: a transicao inteira foi um no-op.
        order.UpdatedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Confirmacao_repetida_e_ignorada_sem_erro()
    {
        var order = CreateOrder();
        order.MarkAsPaid(Now);
        order.Confirm(Now);

        order.Confirm(Now.AddSeconds(10)).ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    // -------------------------------------------------------- eventos fora de ordem

    [Fact]
    public void Confirmacao_antes_do_pagamento_e_ignorada()
    {
        var order = CreateOrder();

        // StockReserved chegando antes de PaymentApproved: filas diferentes nao garantem
        // ordem global entre si. O pedido precisa continuar pendente, e nao pular
        // direto para confirmado sem nunca ter sido pago.
        order.Confirm(Now).ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.PendingPayment);
    }

    [Fact]
    public void Pedido_ja_recusado_nao_pode_ser_pago_depois()
    {
        var order = CreateOrder();
        order.MarkPaymentAsFailed("Cartao recusado.", Now);

        order.MarkAsPaid(Now.AddMinutes(1)).ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.PaymentFailed);
    }

    [Fact]
    public void Pedido_cancelado_nao_volta_para_confirmado()
    {
        var order = CreateOrder();
        order.MarkAsPaid(Now);
        order.Cancel("Sem estoque.", Now.AddSeconds(5));

        order.Confirm(Now.AddSeconds(10)).ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    // ------------------------------------------------------------ caminhos de falha

    [Fact]
    public void Recusa_de_pagamento_registra_o_motivo()
    {
        var order = CreateOrder();

        order.MarkPaymentAsFailed("Saldo insuficiente.", Now).ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.PaymentFailed);
        order.StatusReason.ShouldBe("Saldo insuficiente.");
    }

    [Fact]
    public void Falta_de_estoque_apos_o_pagamento_cancela_o_pedido()
    {
        var order = CreateOrder();
        order.MarkAsPaid(Now);

        // Este e o cenario que exige compensacao: o cliente ja pagou.
        order.Cancel("Estoque insuficiente para o produto X.", Now.AddSeconds(2)).ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.StatusReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Pedido_nao_pago_nao_pode_ser_cancelado_por_falta_de_estoque()
    {
        var order = CreateOrder();

        // Sem pagamento nao houve reserva, entao nao ha o que compensar. O caminho
        // correto para um pedido pendente e MarkPaymentAsFailed.
        order.Cancel("Sem estoque.", Now).ShouldBeFalse();
        order.Status.ShouldBe(OrderStatus.PendingPayment);
    }

    // ---------------------------------------------------------------------- helpers

    private static Order.Domain.Entities.Order CreateOrder(params OrderItem[] items)
    {
        var orderItems = items.Length > 0
            ? items
            : [new OrderItem(Guid.NewGuid(), "Produto de teste", 100m, 1)];

        // O instante de criacao e injetado para que os testes de carimbo de tempo
        // sejam deterministicos, sem depender do relogio da maquina.
        return new Order.Domain.Entities.Order(Guid.NewGuid(), Guid.NewGuid(), CreateAddress(), orderItems, Now);
    }

    private static AddressSnapshot CreateAddress()
        => new("Rua das Flores", "123", "Sao Paulo", "SP", "01234-567", "Brasil");
}
