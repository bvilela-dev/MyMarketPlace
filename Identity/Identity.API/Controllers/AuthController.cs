using Identity.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints de autenticacao: cadastro, login e renovacao de token.
/// </summary>
/// <remarks>
/// <para>
/// Todos sao <c>[AllowAnonymous]</c> pelo motivo obvio: sao a porta de entrada, quem os
/// chama ainda nao tem token.
/// </para>
/// <para>
/// Repare como o controller e fino — recebe o comando e repassa ao MediatR. Nenhuma
/// regra de negocio aqui: essa e a fronteira entre "protocolo HTTP" e "caso de uso".
/// O mesmo comando poderia ser disparado por um consumidor de fila ou por um job, sem
/// nenhuma duplicacao de logica.
/// </para>
/// </remarks>
/// <param name="sender">Mediator usado para despachar os comandos.</param>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Cadastra um novo usuario e ja devolve o par de tokens.
    /// </summary>
    /// <param name="command">Dados do cadastro.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Tokens de acesso do usuario recem-criado.</returns>
    /// <response code="200">Usuario cadastrado.</response>
    /// <response code="400">Dados invalidos (nome, e-mail ou senha).</response>
    /// <response code="409">E-mail ja cadastrado.</response>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AuthResponse> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    /// <summary>
    /// Autentica um usuario e devolve um novo par de tokens.
    /// </summary>
    /// <param name="command">Credenciais.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Tokens de acesso.</returns>
    /// <response code="200">Autenticado.</response>
    /// <response code="400">Payload invalido.</response>
    /// <response code="401">Credenciais invalidas.</response>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<AuthResponse> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);

    /// <summary>
    /// Troca um refresh token valido por um novo par de tokens.
    /// </summary>
    /// <remarks>
    /// O refresh token apresentado e revogado no processo (rotacao) — usa-lo duas vezes
    /// resulta em 401.
    /// </remarks>
    /// <param name="command">Refresh token atual.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Novo par de tokens.</returns>
    /// <response code="200">Tokens renovados.</response>
    /// <response code="401">Refresh token invalido, expirado ou ja utilizado.</response>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<AuthResponse> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
        => sender.Send(command, cancellationToken);
}
