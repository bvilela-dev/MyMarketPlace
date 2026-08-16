using Catalog.Domain.Entities;
using Marketplace.SharedKernel.Exceptions;

namespace Catalog.UnitTests.Domain;

/// <summary>
/// Testes das invariantes do agregado <see cref="Product"/>.
/// </summary>
public sealed class ProductTests
{
    [Fact]
    public void Produto_valido_e_criado_com_os_dados_normalizados()
    {
        var product = new Product(Guid.NewGuid(), "  Teclado  ", "  Mecanico  ", 349.90m, 10);

        product.Name.ShouldBe("Teclado");
        product.Description.ShouldBe("Mecanico");
        product.Price.ShouldBe(349.90m);
        product.AvailableQuantity.ShouldBe(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Preco_nao_positivo_e_rejeitado(decimal preco)
    {
        var criar = () => new Product(Guid.NewGuid(), "Teclado", "desc", preco, 1);

        criar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Quantidade_negativa_e_rejeitada()
    {
        var criar = () => new Product(Guid.NewGuid(), "Teclado", "desc", 10m, -1);

        criar.ShouldThrow<BusinessRuleException>();
    }

    [Fact]
    public void Nome_vazio_e_rejeitado()
    {
        var criar = () => new Product(Guid.NewGuid(), "   ", "desc", 10m, 1);

        criar.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Alterar_preco_para_valor_valido_funciona()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", "desc", 349.90m, 10);

        product.ChangePrice(299.90m);

        product.Price.ShouldBe(299.90m);
    }

    [Fact]
    public void Alterar_preco_para_zero_e_rejeitado()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", "desc", 349.90m, 10);

        var alterar = () => product.ChangePrice(0m);

        alterar.ShouldThrow<BusinessRuleException>();
        // Estado preservado: a tentativa invalida nao pode deixar o objeto corrompido.
        product.Price.ShouldBe(349.90m);
    }

    [Fact]
    public void Ajuste_de_quantidade_soma_e_subtrai()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", "desc", 10m, 5);

        product.AdjustAvailableQuantity(3);
        product.AvailableQuantity.ShouldBe(8);

        product.AdjustAvailableQuantity(-8);
        product.AvailableQuantity.ShouldBe(0);
    }

    [Fact]
    public void Ajuste_que_deixaria_a_quantidade_negativa_e_rejeitado()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", "desc", 10m, 5);

        var ajustar = () => product.AdjustAvailableQuantity(-6);

        ajustar.ShouldThrow<BusinessRuleException>();
        product.AvailableQuantity.ShouldBe(5);
    }

    [Fact]
    public void Preco_usa_decimal_e_preserva_centavos()
    {
        // Com double, 0.1 + 0.2 != 0.3. Em dinheiro isso vira divergencia de fechamento.
        var product = new Product(Guid.NewGuid(), "Item", "desc", 0.1m, 1);
        product.ChangePrice(product.Price + 0.2m);

        product.Price.ShouldBe(0.3m);
    }
}
