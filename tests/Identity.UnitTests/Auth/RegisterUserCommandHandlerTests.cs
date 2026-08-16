using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Domain.Entities;
using Identity.UnitTests.Infrastructure;
using Marketplace.SharedKernel.Exceptions;
using NSubstitute;

namespace Identity.UnitTests.Auth;

/// <summary>
/// Testes do cadastro de usuario.
/// </summary>
/// <remarks>
/// O primeiro teste desta classe cobre exatamente o bug que existia no projeto: a
/// checagem de duplicidade usava o e-mail cru enquanto a gravacao usava o normalizado.
/// Um teste que falha antes da correcao e passa depois — o unico tipo que realmente
/// prova que o defeito foi resolvido.
/// </remarks>
public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Email_com_maiusculas_e_reconhecido_como_duplicado()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = CreateHandler(context);

        await handler.Handle(new RegisterUserCommand("Ana", "ana@teste.com", "senha-forte-123"), TestContext.Current.CancellationToken);

        // Antes da correcao esta segunda chamada passava pela checagem e so estourava
        // no indice unico do Postgres — devolvendo HTTP 500 em vez de 409.
        var duplicado = async () => await handler.Handle(
            new RegisterUserCommand("Ana Maria", "ANA@Teste.COM", "outra-senha-123"),
            TestContext.Current.CancellationToken);

        await duplicado.ShouldThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Email_com_espacos_nas_pontas_e_normalizado_antes_de_gravar()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = CreateHandler(context);

        var resposta = await handler.Handle(
            new RegisterUserCommand("Bruno", "  Bruno@Teste.com  ", "senha-forte-123"),
            TestContext.Current.CancellationToken);

        resposta.Email.ShouldBe("bruno@teste.com");
        context.Users.Single().Email.ShouldBe("bruno@teste.com");
    }

    [Fact]
    public async Task Senha_nunca_e_gravada_em_texto_puro()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = CreateHandler(context);

        await handler.Handle(new RegisterUserCommand("Ana", "ana@teste.com", "senha-secreta"), TestContext.Current.CancellationToken);

        var user = context.Users.Single();
        user.PasswordHash.ShouldNotBe("senha-secreta");
        user.PasswordHash.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Cadastro_enfileira_o_evento_de_integracao_no_outbox()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = CreateHandler(context);

        await handler.Handle(new RegisterUserCommand("Ana", "ana@teste.com", "senha-forte-123"), TestContext.Current.CancellationToken);

        // O ponto central do padrao Outbox: o evento e gravado na MESMA transacao do
        // usuario. Se este teste falhar, o e-mail de boas-vindas pode sumir sempre que
        // o RabbitMQ estiver indisponivel no instante do cadastro.
        var outbox = context.OutboxMessages.Single();
        outbox.Type.ShouldContain("UserCreatedEvent");
        outbox.Payload.ShouldContain("ana@teste.com");
        outbox.ProcessedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Cadastro_grava_o_hash_do_refresh_token_e_nao_o_valor_original()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = CreateHandler(context);

        var resposta = await handler.Handle(
            new RegisterUserCommand("Ana", "ana@teste.com", "senha-forte-123"),
            TestContext.Current.CancellationToken);

        var tokenGravado = context.RefreshTokens.Single();

        // Se o banco vazar, os refresh tokens roubados nao servem para nada.
        tokenGravado.TokenHash.ShouldNotBe(resposta.RefreshToken);
        tokenGravado.TokenHash.ShouldBe(FakeTokenService.Hash(resposta.RefreshToken));
    }

    private static RegisterUserCommandHandler CreateHandler(IIdentityDbContext context)
    {
        // O hasher e substituido por um duble para o teste nao pagar os ~250 ms do
        // BCrypt em cada caso. O comportamento do BCrypt em si e testado a parte.
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns(call => $"hash::{call.Arg<string>()}");
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => call.ArgAt<string>(1) == $"hash::{call.ArgAt<string>(0)}");

        return new RegisterUserCommandHandler(context, hasher, new FakeTokenService());
    }
}
