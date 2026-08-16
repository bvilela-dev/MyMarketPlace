using Identity.Infrastructure.Security;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Testes do hash de senha com BCrypt.
/// </summary>
public sealed class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Senha_correta_e_verificada_com_sucesso()
    {
        var hash = _hasher.Hash("senha-forte-123");

        _hasher.Verify("senha-forte-123", hash).ShouldBeTrue();
    }

    [Fact]
    public void Senha_incorreta_e_rejeitada()
    {
        var hash = _hasher.Hash("senha-forte-123");

        _hasher.Verify("senha-errada", hash).ShouldBeFalse();
    }

    [Fact]
    public void Mesma_senha_gera_hashes_diferentes()
    {
        // O BCrypt gera um salt aleatorio por chamada e o embute no hash. Por isso
        // duas contas com a mesma senha tem hashes distintos — e uma rainbow table
        // nao ajuda quem obtiver o banco.
        _hasher.Hash("senha-forte-123").ShouldNotBe(_hasher.Hash("senha-forte-123"));
    }

    [Fact]
    public void Hash_malformado_nao_derruba_a_verificacao()
    {
        // Cenario real: o hash "dummy" usado no login para igualar o tempo de resposta,
        // ou um registro corrompido. Deixar a excecao subir viraria HTTP 500 e
        // revelaria o estado do registro para quem esta sondando.
        _hasher.Verify("qualquer-senha", "isto-nao-e-um-hash-bcrypt").ShouldBeFalse();
    }
}
