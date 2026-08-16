using Identity.Application.Abstractions;
using Identity.Application.Auth;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Identity.UnitTests.Infrastructure;
using Marketplace.SharedKernel.Exceptions;

namespace Identity.UnitTests.Auth;

/// <summary>
/// Testes de login e de rotacao de refresh token.
/// </summary>
public sealed class LoginAndRefreshTests
{
    private static readonly IPasswordHasher Hasher = new BcryptPasswordHasher();

    [Fact]
    public async Task Login_com_credenciais_validas_devolve_tokens()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        await SeedUserAsync(context, "ana@teste.com", "senha-forte-123");

        var handler = new LoginCommandHandler(context, Hasher, new FakeTokenService());

        var resposta = await handler.Handle(
            new LoginCommand("ana@teste.com", "senha-forte-123"),
            TestContext.Current.CancellationToken);

        resposta.AccessToken.ShouldNotBeNullOrWhiteSpace();
        resposta.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        resposta.Email.ShouldBe("ana@teste.com");
    }

    [Fact]
    public async Task Login_aceita_email_digitado_com_maiusculas()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        await SeedUserAsync(context, "ana@teste.com", "senha-forte-123");

        var handler = new LoginCommandHandler(context, Hasher, new FakeTokenService());

        // A mesma normalizacao aplicada no cadastro precisa valer no login — caso
        // contrario o usuario "some" ao digitar o e-mail de forma diferente.
        var resposta = await handler.Handle(
            new LoginCommand("  ANA@Teste.com ", "senha-forte-123"),
            TestContext.Current.CancellationToken);

        resposta.Email.ShouldBe("ana@teste.com");
    }

    [Fact]
    public async Task Senha_incorreta_e_usuario_inexistente_produzem_a_mesma_mensagem()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        await SeedUserAsync(context, "ana@teste.com", "senha-forte-123");

        var handler = new LoginCommandHandler(context, Hasher, new FakeTokenService());

        var senhaErrada = await Should.ThrowAsync<AuthenticationFailedException>(
            async () => await handler.Handle(new LoginCommand("ana@teste.com", "errada"), TestContext.Current.CancellationToken));

        var usuarioInexistente = await Should.ThrowAsync<AuthenticationFailedException>(
            async () => await handler.Handle(new LoginCommand("ninguem@teste.com", "qualquer"), TestContext.Current.CancellationToken));

        // Mensagens diferentes permitiriam enumerar quais e-mails tem conta no sistema.
        usuarioInexistente.Message.ShouldBe(senhaErrada.Message);
    }

    [Fact]
    public async Task Refresh_rotaciona_o_token_e_revoga_o_anterior()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var tokenService = new FakeTokenService();
        await SeedUserAsync(context, "ana@teste.com", "senha-forte-123");

        var login = await new LoginCommandHandler(context, Hasher, tokenService)
            .Handle(new LoginCommand("ana@teste.com", "senha-forte-123"), TestContext.Current.CancellationToken);

        var refresh = await new RefreshTokenCommandHandler(context, tokenService)
            .Handle(new RefreshTokenCommand(login.RefreshToken), TestContext.Current.CancellationToken);

        refresh.RefreshToken.ShouldNotBe(login.RefreshToken);

        var tokenAntigo = context.RefreshTokens.Single(token => token.TokenHash == FakeTokenService.Hash(login.RefreshToken));
        tokenAntigo.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_token_reutilizado_e_rejeitado()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var tokenService = new FakeTokenService();
        await SeedUserAsync(context, "ana@teste.com", "senha-forte-123");

        var login = await new LoginCommandHandler(context, Hasher, tokenService)
            .Handle(new LoginCommand("ana@teste.com", "senha-forte-123"), TestContext.Current.CancellationToken);

        var handler = new RefreshTokenCommandHandler(context, tokenService);
        await handler.Handle(new RefreshTokenCommand(login.RefreshToken), TestContext.Current.CancellationToken);

        // Segunda tentativa com o MESMO token: e o sinal classico de token vazado.
        var reuso = async () => await handler.Handle(
            new RefreshTokenCommand(login.RefreshToken),
            TestContext.Current.CancellationToken);

        await reuso.ShouldThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Refresh_token_desconhecido_e_rejeitado()
    {
        await using var context = InMemoryIdentityDbContext.Create();
        var handler = new RefreshTokenCommandHandler(context, new FakeTokenService());

        var invalido = async () => await handler.Handle(
            new RefreshTokenCommand("token-que-nunca-existiu"),
            TestContext.Current.CancellationToken);

        await invalido.ShouldThrowAsync<AuthenticationFailedException>();
    }

    private static async Task SeedUserAsync(IdentityDbContext context, string email, string password)
    {
        var handler = new RegisterUserCommandHandler(context, Hasher, new FakeTokenService());
        await handler.Handle(new RegisterUserCommand("Usuario de Teste", email, password), TestContext.Current.CancellationToken);
    }
}
