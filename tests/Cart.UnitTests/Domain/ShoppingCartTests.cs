using Cart.Domain.Entities;
using Marketplace.SharedKernel.Exceptions;

namespace Cart.UnitTests.Domain;

/// <summary>
/// Testes da consolidacao do carrinho.
/// </summary>
public sealed class ShoppingCartTests
{
    private static readonly Guid Usuario = Guid.NewGuid();

    [Fact]
    public void Carrinho_vazio_tem_total_zero()
    {
        var cart = ShoppingCart.Create(Usuario, []);

        cart.Items.ShouldBeEmpty();
        cart.Total.ShouldBe(0m);
    }

    [Fact]
    public void Total_e_a_soma_das_linhas()
    {
        var cart = ShoppingCart.Create(Usuario, [
            new CartItem(Guid.NewGuid(), "Teclado", 349.90m, 2),
            new CartItem(Guid.NewGuid(), "Mouse", 219.90m, 1)
        ]);

        cart.Total.ShouldBe(919.70m);
    }

    [Fact]
    public void Linhas_repetidas_do_mesmo_produto_sao_consolidadas()
    {
        var produto = Guid.NewGuid();

        // Cenario real: o cliente clica em "adicionar" tres vezes e o front envia
        // tres linhas separadas do mesmo item.
        var cart = ShoppingCart.Create(Usuario, [
            new CartItem(produto, "Teclado", 349.90m, 1),
            new CartItem(produto, "Teclado", 349.90m, 2)
        ]);

        var item = cart.Items.ShouldHaveSingleItem();
        item.Quantity.ShouldBe(3);
        cart.Total.ShouldBe(1049.70m);
    }

    [Fact]
    public void Consolidacao_conta_produtos_distintos_e_nao_linhas_enviadas()
    {
        var produto = Guid.NewGuid();

        // 150 linhas, mas de um unico produto: nao pode estourar o limite de 100.
        var itens = Enumerable.Range(0, 150)
            .Select(_ => new CartItem(produto, "Teclado", 10m, 1))
            .ToArray();

        var cart = ShoppingCart.Create(Usuario, itens);

        cart.Items.Count.ShouldBe(1);
        cart.Items.Single().Quantity.ShouldBe(150);
    }

    [Fact]
    public void Carrinho_acima_do_limite_de_produtos_distintos_e_rejeitado()
    {
        var itens = Enumerable.Range(0, ShoppingCart.MaxItems + 1)
            .Select(_ => new CartItem(Guid.NewGuid(), "Produto", 10m, 1))
            .ToArray();

        var criar = () => ShoppingCart.Create(Usuario, itens);

        criar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Carrinho_exatamente_no_limite_e_aceito()
    {
        var itens = Enumerable.Range(0, ShoppingCart.MaxItems)
            .Select(_ => new CartItem(Guid.NewGuid(), "Produto", 10m, 1))
            .ToArray();

        ShoppingCart.Create(Usuario, itens).Items.Count.ShouldBe(ShoppingCart.MaxItems);
    }

    [Fact]
    public void Carrinho_guarda_o_dono_e_o_momento_da_alteracao()
    {
        var antes = DateTime.UtcNow;

        var cart = ShoppingCart.Create(Usuario, [new CartItem(Guid.NewGuid(), "Item", 1m, 1)]);

        cart.UserId.ShouldBe(Usuario);
        cart.UpdatedAtUtc.ShouldBeGreaterThanOrEqualTo(antes);
    }
}
