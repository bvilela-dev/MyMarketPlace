using Identity.Application.Models;
using Identity.Application.Users;
using Marketplace.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints de perfil de usuario.
/// </summary>
/// <param name="sender">Mediator usado para despachar comandos e consultas.</param>
/// <param name="currentUser">Usuario autenticado na requisicao atual.</param>
[ApiController]
[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Retorna o perfil do usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Endpoint "me": o identificador vem do token, nunca da URL. E a forma mais segura
    /// de expor o proprio perfil, porque nao existe parametro que o cliente possa
    /// manipular.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil do usuario autenticado.</returns>
    /// <response code="200">Perfil encontrado.</response>
    /// <response code="401">Token ausente, invalido ou expirado.</response>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetUserByIdQuery(currentUser.RequireUserId()), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Retorna o perfil de um usuario pelo identificador.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Correcao de seguranca (IDOR).</b> Antes, este endpoint era
    /// <c>[AllowAnonymous]</c>: qualquer pessoa lia o nome e o e-mail de qualquer
    /// usuario, alem da lista completa de enderecos, apenas variando o GUID da URL.
    /// </para>
    /// <para>
    /// Agora o acesso exige autenticacao e o <c>EnsureOwns</c> confirma que o token
    /// pertence ao usuario solicitado. Num sistema real este seria o ponto de liberar o
    /// acesso tambem para um papel administrativo.
    /// </para>
    /// </remarks>
    /// <param name="id">Identificador do usuario.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil do usuario.</returns>
    /// <response code="200">Perfil encontrado.</response>
    /// <response code="403">O usuario autenticado nao e o dono do perfil solicitado.</response>
    /// <response code="404">Usuario inexistente.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        currentUser.EnsureOwns(id);

        var user = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Cadastra um endereco no perfil do usuario.
    /// </summary>
    /// <remarks>
    /// Outra correcao de IDOR: antes, qualquer usuario autenticado podia gravar
    /// enderecos no perfil de terceiros, bastando trocar o GUID da rota.
    /// </remarks>
    /// <param name="id">Identificador do usuario.</param>
    /// <param name="request">Dados do endereco.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Endereco criado.</returns>
    /// <response code="201">Endereco cadastrado.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="403">O usuario autenticado nao e o dono do perfil informado.</response>
    [HttpPost("{id:guid}/addresses")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddressDto>> AddAddress(Guid id, [FromBody] AddAddressRequest request, CancellationToken cancellationToken)
    {
        currentUser.EnsureOwns(id);

        var address = await sender.Send(
            new AddAddressCommand(id, request.Street, request.Number, request.City, request.State, request.ZipCode, request.Country),
            cancellationToken);

        // 201 + Location: o cliente descobre onde consultar o recurso recem-criado.
        return CreatedAtAction(nameof(GetById), new { id }, address);
    }
}

/// <summary>
/// Corpo da requisicao de cadastro de endereco.
/// </summary>
/// <remarks>
/// Existe separado de <c>AddAddressCommand</c> de proposito: o comando carrega o
/// <c>UserId</c>, que vem da rota (validado contra o token). Se o cliente enviasse o
/// comando inteiro, poderia informar o <c>UserId</c> de outra pessoa no corpo.
/// </remarks>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record AddAddressRequest(string Street, string Number, string City, string State, string ZipCode, string Country);
