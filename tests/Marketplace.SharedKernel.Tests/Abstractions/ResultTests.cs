using Marketplace.SharedKernel.Abstractions;

namespace Marketplace.SharedKernel.Tests.Abstractions;

/// <summary>
/// Testes do tipo <see cref="Result"/>.
/// </summary>
public sealed class ResultTests
{
    [Fact]
    public void Sucesso_sem_valor_nao_tem_erro()
    {
        var resultado = Result.Success();

        resultado.IsSuccess.ShouldBeTrue();
        resultado.IsFailure.ShouldBeFalse();
        resultado.Error.ShouldBeNull();
    }

    [Fact]
    public void Falha_carrega_o_motivo()
    {
        var resultado = Result.Failure("Saldo insuficiente.");

        resultado.IsSuccess.ShouldBeFalse();
        resultado.IsFailure.ShouldBeTrue();
        resultado.Error.ShouldBe("Saldo insuficiente.");
    }

    [Fact]
    public void Sucesso_com_valor_preserva_o_valor()
    {
        var resultado = Result<int>.Success(42);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe(42);
    }

    [Fact]
    public void Falha_generica_mantem_o_tipo_e_nao_tem_valor()
    {
        // O 'new' em Result<T>.Failure existe justamente para isto: sem ele, a chamada
        // devolveria um Result nao generico e nao compilaria no ponto de uso.
        Result<string> resultado = Result<string>.Failure("Nao encontrado.");

        resultado.IsFailure.ShouldBeTrue();
        resultado.Value.ShouldBeNull();
        resultado.Error.ShouldBe("Nao encontrado.");
    }
}
