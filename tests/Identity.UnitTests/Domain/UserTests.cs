using Identity.Domain.Entities;
using Identity.Domain.Events;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Testes das regras do agregado <see cref="User"/>.
/// </summary>
public sealed class UserTests
{
    [Theory]
    [InlineData("ANA@TESTE.COM", "ana@teste.com")]
    [InlineData("  ana@teste.com  ", "ana@teste.com")]
    [InlineData("Ana.Maria@Teste.COM", "ana.maria@teste.com")]
    public void NormalizeEmail_padroniza_caixa_e_espacos(string entrada, string esperado)
        => User.NormalizeEmail(entrada).ShouldBe(esperado);

    [Fact]
    public void Criar_usuario_dispara_evento_de_dominio()
    {
        var user = new User(Guid.NewGuid(), "Ana", "ana@teste.com", "hash");

        var evento = user.DomainEvents.ShouldHaveSingleItem();
        evento.ShouldBeOfType<UserCreatedDomainEvent>()
            .Email.ShouldBe("ana@teste.com");
    }

    [Theory]
    [InlineData("", "ana@teste.com", "hash")]
    [InlineData("Ana", "", "hash")]
    [InlineData("Ana", "ana@teste.com", "")]
    [InlineData("   ", "ana@teste.com", "hash")]
    public void Campos_obrigatorios_vazios_sao_rejeitados(string nome, string email, string hash)
    {
        // Guardas no construtor garantem que nao existe User invalido em memoria,
        // mesmo que a camada de aplicacao esqueca de validar.
        var criar = () => new User(Guid.NewGuid(), nome, email, hash);

        criar.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Colecao_de_enderecos_nao_pode_ser_alterada_por_fora()
    {
        var user = new User(Guid.NewGuid(), "Ana", "ana@teste.com", "hash");
        user.AddAddress("Rua A", "1", "Sao Paulo", "SP", "01000-000", "Brasil");

        // Encapsulamento: a unica forma de incluir endereco e pelo metodo do agregado.
        user.Addresses.ShouldBeAssignableTo<IReadOnlyCollection<Address>>();
        user.Addresses.Count.ShouldBe(1);
    }

    [Fact]
    public void Revogar_todos_os_tokens_invalida_apenas_os_ativos()
    {
        var agora = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var user = new User(Guid.NewGuid(), "Ana", "ana@teste.com", "hash");

        var ativo = user.AddRefreshToken("hash-ativo", agora.AddDays(30));
        var expirado = user.AddRefreshToken("hash-expirado", agora.AddDays(-1));

        user.RevokeAllRefreshTokens(agora);

        ativo.IsRevoked.ShouldBeTrue();
        // Ja estava fora de validade: nao ha o que revogar.
        expirado.IsRevoked.ShouldBeFalse();
        expirado.IsActive(agora).ShouldBeFalse();
    }

    [Fact]
    public void Token_expirado_ou_revogado_nao_e_considerado_ativo()
    {
        var agora = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var user = new User(Guid.NewGuid(), "Ana", "ana@teste.com", "hash");

        var token = user.AddRefreshToken("hash", agora.AddDays(1));
        token.IsActive(agora).ShouldBeTrue();

        token.IsActive(agora.AddDays(2)).ShouldBeFalse();

        token.Revoke();
        token.IsActive(agora).ShouldBeFalse();
    }
}
