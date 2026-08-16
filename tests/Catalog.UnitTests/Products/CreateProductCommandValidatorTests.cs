using Catalog.Application.Products;

namespace Catalog.UnitTests.Products;

/// <summary>
/// Testes das regras de validacao do cadastro de produto.
/// </summary>
/// <remarks>
/// Validador roda antes do handler e antes de qualquer acesso ao banco: e a primeira
/// barreira contra payload malformado.
/// </remarks>
public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Comando_valido_passa()
    {
        var resultado = _validator.Validate(new CreateProductCommand("Teclado", "Mecanico", 349.90m, 10));

        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Nome_vazio_e_reprovado()
    {
        var resultado = _validator.Validate(new CreateProductCommand("", "desc", 10m, 1));

        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Fact]
    public void Preco_zero_e_reprovado()
    {
        var resultado = _validator.Validate(new CreateProductCommand("Teclado", "desc", 0m, 1));

        resultado.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Quantidade_negativa_e_reprovada()
    {
        var resultado = _validator.Validate(new CreateProductCommand("Teclado", "desc", 10m, -1));

        resultado.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Todos_os_erros_sao_reportados_de_uma_vez()
    {
        // Falhar no primeiro erro obrigaria o cliente a corrigir o formulario campo a
        // campo, uma requisicao por vez.
        var resultado = _validator.Validate(new CreateProductCommand("", "desc", -5m, -1));

        resultado.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }
}
