using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Marketplace.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.UnitTests.Persistence;

/// <summary>
/// Testes da reserva de estoque no repositorio.
/// </summary>
/// <remarks>
/// Estes testes cobrem a correcao do bug mais grave que o projeto tinha: a reserva
/// antiga pegava um item arbitrario da tabela e baixava sempre 1 unidade, ignorando
/// completamente quais produtos haviam sido comprados.
/// </remarks>
public sealed class InventoryRepositoryTests
{
    [Fact]
    public async Task Reserva_baixa_a_quantidade_correta_de_cada_produto()
    {
        await using var context = CreateContext();
        var teclado = Guid.NewGuid();
        var mouse = Guid.NewGuid();

        context.StockItems.Add(new StockItem(Guid.NewGuid(), teclado, 10));
        context.StockItems.Add(new StockItem(Guid.NewGuid(), mouse, 10));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new InventoryRepository(context);

        await repository.ReserveAsync(
            new Dictionary<Guid, int> { [teclado] = 3, [mouse] = 1 },
            TestContext.Current.CancellationToken);

        // A versao anterior falharia aqui: baixava 1 unidade de um unico produto.
        context.StockItems.Single(i => i.ProductId == teclado).QuantityAvailable.ShouldBe(7);
        context.StockItems.Single(i => i.ProductId == mouse).QuantityAvailable.ShouldBe(9);
    }

    [Fact]
    public async Task Reserva_e_tudo_ou_nada_quando_um_item_nao_tem_saldo()
    {
        await using var context = CreateContext();
        var comEstoque = Guid.NewGuid();
        var semEstoque = Guid.NewGuid();

        context.StockItems.Add(new StockItem(Guid.NewGuid(), comEstoque, 10));
        context.StockItems.Add(new StockItem(Guid.NewGuid(), semEstoque, 1));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new InventoryRepository(context);

        var reservar = async () => await repository.ReserveAsync(
            new Dictionary<Guid, int> { [comEstoque] = 2, [semEstoque] = 5 },
            TestContext.Current.CancellationToken);

        await reservar.ShouldThrowAsync<BusinessRuleException>();

        // Nenhuma baixa parcial: reservar so o primeiro item deixaria unidades presas
        // para uma venda que nunca vai acontecer.
        context.ChangeTracker.Clear();
        context.StockItems.Single(i => i.ProductId == comEstoque).QuantityAvailable.ShouldBe(10);
    }

    [Fact]
    public async Task Produto_sem_cadastro_de_estoque_falha_com_erro_de_negocio()
    {
        await using var context = CreateContext();
        var repository = new InventoryRepository(context);

        var reservar = async () => await repository.ReserveAsync(
            new Dictionary<Guid, int> { [Guid.NewGuid()] = 1 },
            TestContext.Current.CancellationToken);

        // BusinessRuleException (e nao uma falha tecnica): o consumidor precisa
        // distinguir isso para publicar StockReservationFailed em vez de retentar.
        await reservar.ShouldThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Criacao_de_estoque_e_idempotente()
    {
        await using var context = CreateContext();
        var repository = new InventoryRepository(context);
        var produto = Guid.NewGuid();

        await repository.EnsureStockAsync(produto, 10, TestContext.Current.CancellationToken);
        // Reprocessamento do ProductCreatedEvent: a quantidade NAO pode ser somada
        // de novo, senao cada reentrega inflaria o estoque.
        await repository.EnsureStockAsync(produto, 10, TestContext.Current.CancellationToken);

        var item = context.StockItems.Single(i => i.ProductId == produto);
        item.QuantityAvailable.ShouldBe(10);
        context.StockItems.Count(i => i.ProductId == produto).ShouldBe(1);
    }

    [Fact]
    public async Task Reserva_sem_itens_nao_faz_nada()
    {
        await using var context = CreateContext();
        var repository = new InventoryRepository(context);

        await repository.ReserveAsync(new Dictionary<Guid, int>(), TestContext.Current.CancellationToken);

        context.StockItems.ShouldBeEmpty();
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"inventory-{Guid.NewGuid()}")
            .Options;

        return new InventoryDbContext(options);
    }
}
