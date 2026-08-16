using Inventory.Domain.Entities;
using Marketplace.SharedKernel.Exceptions;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Testes das regras de movimentacao de estoque.
/// </summary>
public sealed class StockItemTests
{
    [Fact]
    public void Estoque_novo_comeca_todo_disponivel()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 10);

        item.QuantityAvailable.ShouldBe(10);
        item.QuantityReserved.ShouldBe(0);
        item.QuantityOnHand.ShouldBe(10);
    }

    [Fact]
    public void Reserva_move_do_disponivel_para_o_reservado_sem_alterar_o_total()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 10);

        item.Reserve(3);

        item.QuantityAvailable.ShouldBe(7);
        item.QuantityReserved.ShouldBe(3);
        // O que existe fisicamente no deposito nao mudou: a mercadoria so ficou separada.
        item.QuantityOnHand.ShouldBe(10);
    }

    [Fact]
    public void Reserva_acima_do_disponivel_e_rejeitada()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 5);

        var reservar = () => item.Reserve(6);

        reservar.ShouldThrow<BusinessRuleException>();
        // Nada foi movido: a operacao invalida nao pode deixar estado parcial.
        item.QuantityAvailable.ShouldBe(5);
        item.QuantityReserved.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserva_de_quantidade_nao_positiva_e_rejeitada(int quantidade)
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 5);

        var reservar = () => item.Reserve(quantidade);

        reservar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Liberacao_devolve_a_reserva_para_o_disponivel()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 10);
        item.Reserve(4);

        // Compensacao: o pedido foi cancelado depois da reserva.
        item.Release(4);

        item.QuantityAvailable.ShouldBe(10);
        item.QuantityReserved.ShouldBe(0);
    }

    [Fact]
    public void Liberar_mais_do_que_o_reservado_e_rejeitado()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 10);
        item.Reserve(2);

        var liberar = () => item.Release(3);

        liberar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Reposicao_aumenta_o_disponivel()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 1);

        item.Replenish(9);

        item.QuantityAvailable.ShouldBe(10);
    }

    [Fact]
    public void Quantidade_inicial_negativa_e_rejeitada()
    {
        var criar = () => new StockItem(Guid.NewGuid(), Guid.NewGuid(), -1);

        criar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Reservar_ate_zerar_o_disponivel_e_permitido()
    {
        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 3);

        item.Reserve(3);

        item.QuantityAvailable.ShouldBe(0);
        // A proxima reserva precisa falhar — e o cenario que gera StockReservationFailed.
        Should.Throw<BusinessRuleException>(() => item.Reserve(1));
    }
}
