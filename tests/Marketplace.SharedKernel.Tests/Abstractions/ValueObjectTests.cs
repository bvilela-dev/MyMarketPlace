using Marketplace.SharedKernel.Abstractions;

namespace Marketplace.SharedKernel.Tests.Abstractions;

/// <summary>
/// Testes da igualdade estrutural dos objetos de valor.
/// </summary>
public sealed class ValueObjectTests
{
    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;

        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Weight(decimal amount, string unit) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return unit;
        }
    }

    [Fact]
    public void Mesmos_componentes_sao_iguais()
    {
        new Money(10m, "BRL").Equals(new Money(10m, "BRL")).ShouldBeTrue();
        (new Money(10m, "BRL") == new Money(10m, "BRL")).ShouldBeTrue();
    }

    [Fact]
    public void Componentes_diferentes_nao_sao_iguais()
    {
        (new Money(10m, "BRL") != new Money(10m, "USD")).ShouldBeTrue();
        (new Money(10m, "BRL") != new Money(11m, "BRL")).ShouldBeTrue();
    }

    [Fact]
    public void Tipos_diferentes_com_os_mesmos_valores_nao_sao_iguais()
    {
        // Sem a checagem de tipo exato, R$ 10,00 seria "igual" a 10 quilos.
        new Money(10m, "kg").Equals(new Weight(10m, "kg")).ShouldBeFalse();
    }

    [Fact]
    public void Objetos_iguais_produzem_o_mesmo_hash_code()
    {
        // Contrato obrigatorio do .NET. Se falhar, o objeto se comporta de forma
        // imprevisivel dentro de Dictionary e HashSet.
        new Money(10m, "BRL").GetHashCode().ShouldBe(new Money(10m, "BRL").GetHashCode());
    }

    [Fact]
    public void Funciona_como_chave_de_dicionario()
    {
        var mapa = new Dictionary<ValueObject, string> { [new Money(10m, "BRL")] = "dez reais" };

        mapa[new Money(10m, "BRL")].ShouldBe("dez reais");
    }

    [Fact]
    public void Comparacao_com_nulo_e_segura()
    {
        Money? nulo = null;

        (nulo == null).ShouldBeTrue();
        (new Money(1m, "BRL") == null).ShouldBeFalse();
        new Money(1m, "BRL").Equals(null).ShouldBeFalse();
    }
}
