using Catalog.Application.Products;
using Marketplace.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

/// <summary>
/// Endpoints de consulta e cadastro de produtos.
/// </summary>
/// <param name="sender">Mediator usado para despachar comandos e consultas.</param>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lista produtos de forma paginada.
    /// </summary>
    /// <remarks>
    /// Publico: e a vitrine da loja, precisa funcionar sem login. E o ponto de partida
    /// da demonstracao — daqui saem os <c>productId</c> usados no carrinho e no pedido.
    /// </remarks>
    /// <param name="page">Numero da pagina (base 1).</param>
    /// <param name="pageSize">Itens por pagina (maximo 100).</param>
    /// <param name="search">Termo de busca opcional aplicado ao nome.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Pagina de produtos.</returns>
    /// <response code="200">Lista retornada.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<ProductDto>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => sender.Send(new ListProductsQuery(page, pageSize, search), cancellationToken);

    /// <summary>
    /// Busca um produto pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Dados do produto.</returns>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Produto inexistente.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ProductDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Cadastra um produto no catalogo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exige autenticacao: cadastrar produto e operacao de vendedor/administrador, nao
    /// de visitante.
    /// </para>
    /// <para>
    /// Num sistema real haveria tambem autorizacao por papel
    /// (<c>[Authorize(Roles = "Seller")]</c>). Como o Identity deste projeto ainda nao
    /// modela papeis, o controle para na autenticacao — e este comentario marca
    /// exatamente onde a verificacao de papel entraria.
    /// </para>
    /// <para>
    /// Efeito colateral relevante: a criacao publica um <c>ProductCreatedEvent</c>, e o
    /// Inventory cria a linha de estoque correspondente de forma assincrona.
    /// </para>
    /// </remarks>
    /// <param name="command">Dados do produto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Produto criado.</returns>
    /// <response code="201">Produto cadastrado.</response>
    /// <response code="400">Dados invalidos.</response>
    /// <response code="401">Token ausente ou invalido.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
