using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Lista produtos de forma paginada, com busca opcional por nome.
/// </summary>
/// <param name="Page">Numero da pagina (base 1).</param>
/// <param name="PageSize">Quantidade de itens por pagina.</param>
/// <param name="Search">Termo de busca aplicado ao nome do produto.</param>
public sealed record ListProductsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<ProductDto>>;
