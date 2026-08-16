using Marketplace.Contracts.Grpc;
using Marketplace.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Order.Application.Abstractions;
using Order.Application.Orders;
using Order.Infrastructure.Persistence;

namespace Order.UnitTests.Orders;

/// <summary>
/// Testes do caso de uso de criacao de pedido.
/// </summary>
/// <remarks>
/// Os clientes gRPC sao substituidos por dubles: o teste verifica a <b>orquestracao</b>
/// (validou o endereco? conferiu o estoque? gravou o evento no outbox?), nao a
/// comunicacao em si. Testar a chamada gRPC de verdade exigiria subir Identity e
/// Catalog — isso e teste de integracao, com outro custo e outra frequencia.
/// </remarks>
public sealed class CreateOrderCommandHandlerTests
{
    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly Guid Endereco = Guid.NewGuid();

    [Fact]
    public async Task Pedido_valido_e_gravado_com_o_total_calculado_pelo_servidor()
    {
        var produto = Guid.NewGuid();
        await using var context = CreateContext();

        var handler = CreateHandler(context, enderecoValido: true, (produto, "Teclado", 349.90m, 10));

        var resposta = await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [new CreateOrderItemRequest(produto, 2)]),
            TestContext.Current.CancellationToken);

        // 349,90 x 2 — preco vindo do Catalog, nunca do cliente.
        resposta.Total.ShouldBe(699.80m);
        resposta.Status.ShouldBe("PendingPayment");

        context.Orders.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Criacao_de_pedido_enfileira_o_evento_no_outbox()
    {
        var produto = Guid.NewGuid();
        await using var context = CreateContext();

        var handler = CreateHandler(context, enderecoValido: true, (produto, "Teclado", 100m, 10));

        await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [new CreateOrderItemRequest(produto, 1)]),
            TestContext.Current.CancellationToken);

        var outbox = context.OutboxMessages.Single();
        outbox.Type.ShouldContain("OrderCreatedEvent");
        outbox.ProcessedOnUtc.ShouldBeNull();
        // Os itens precisam viajar no evento: sem eles o Inventory nao sabe o que reservar.
        outbox.Payload.ShouldContain(produto.ToString());
    }

    [Fact]
    public async Task Endereco_invalido_impede_a_criacao_do_pedido()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context, enderecoValido: false, (Guid.NewGuid(), "Teclado", 100m, 10));

        var criar = async () => await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [new CreateOrderItemRequest(Guid.NewGuid(), 1)]),
            TestContext.Current.CancellationToken);

        await criar.ShouldThrowAsync<BusinessRuleException>();
        context.Orders.ShouldBeEmpty();
    }

    [Fact]
    public async Task Estoque_insuficiente_impede_a_criacao_do_pedido()
    {
        var produto = Guid.NewGuid();
        await using var context = CreateContext();

        var handler = CreateHandler(context, enderecoValido: true, (produto, "Teclado", 100m, 2));

        var criar = async () => await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [new CreateOrderItemRequest(produto, 5)]),
            TestContext.Current.CancellationToken);

        await criar.ShouldThrowAsync<BusinessRuleException>();

        // Nada gravado: nem o pedido, nem o evento.
        context.Orders.ShouldBeEmpty();
        context.OutboxMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linhas_repetidas_do_mesmo_produto_sao_somadas_antes_da_checagem_de_estoque()
    {
        var produto = Guid.NewGuid();
        await using var context = CreateContext();

        // Estoque de 5 unidades; o cliente envia 3 linhas de 2 = 6 no total.
        var handler = CreateHandler(context, enderecoValido: true, (produto, "Teclado", 100m, 5));

        var criar = async () => await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [
                new CreateOrderItemRequest(produto, 2),
                new CreateOrderItemRequest(produto, 2),
                new CreateOrderItemRequest(produto, 2)
            ]),
            TestContext.Current.CancellationToken);

        // Sem o agrupamento, cada linha passaria sozinha na checagem (2 <= 5) e o
        // pedido levaria 6 unidades de um estoque de 5.
        await criar.ShouldThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Endereco_e_congelado_no_pedido_no_momento_da_compra()
    {
        var produto = Guid.NewGuid();
        await using var context = CreateContext();

        var handler = CreateHandler(context, enderecoValido: true, (produto, "Teclado", 100m, 10));

        await handler.Handle(
            new CreateOrderCommand(Usuario, Endereco, [new CreateOrderItemRequest(produto, 1)]),
            TestContext.Current.CancellationToken);

        var order = context.Orders.Single();
        order.AddressSnapshot.City.ShouldBe("Sao Paulo");
        order.AddressSnapshot.ZipCode.ShouldBe("01234-567");
    }

    // ---------------------------------------------------------------------- helpers

    private static CreateOrderCommandHandler CreateHandler(
        OrderDbContext context,
        bool enderecoValido,
        params (Guid Id, string Nome, decimal Preco, int Estoque)[] produtos)
    {
        var identity = Substitute.For<IIdentityGrpcClient>();
        identity.ValidateUserAddressAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new UserAddressValidationDto(
                enderecoValido, Usuario, Endereco,
                "Rua das Flores", "123", "Sao Paulo", "SP", "01234-567", "Brasil"));

        var catalog = Substitute.For<ICatalogGrpcClient>();
        foreach (var (id, nome, preco, estoque) in produtos)
        {
            catalog.GetProductAsync(id, Arg.Any<CancellationToken>())
                .Returns(new ProductDetailsDto(id, nome, preco, estoque));
        }

        return new CreateOrderCommandHandler(
            context,
            catalog,
            identity,
            NullLogger<CreateOrderCommandHandler>.Instance);
    }

    private static OrderDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase($"order-{Guid.NewGuid()}")
            .Options;

        return new OrderDbContext(options);
    }
}
