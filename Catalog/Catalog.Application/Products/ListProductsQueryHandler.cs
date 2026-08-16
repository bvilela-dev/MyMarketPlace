using Catalog.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

/// <summary>
/// Lista produtos paginados com filtro opcional por nome.
/// </summary>
/// <remarks>
/// <para>
/// Esta consulta <b>nao</b> passa pelo cache: o resultado depende de pagina, tamanho e
/// termo de busca, o que geraria uma explosao de chaves no Redis com baixissima taxa de
/// acerto. Cache-aside compensa em leitura por chave (o <c>GetById</c>), nao em listagem
/// filtrada.
/// </para>
/// <para>
/// A busca usa <c>EF.Functions.Like</c> sobre o nome em minusculas. Poderia usar o
/// <c>ILIKE</c> nativo do Postgres via <c>EF.Functions.ILike</c>, mas isso exigiria
/// referenciar o pacote do Npgsql aqui — e a camada de aplicacao ficaria amarrada ao
/// banco escolhido. O <c>Like</c> e traduzido por qualquer provider relacional.
/// </para>
/// <para>
/// <b>Custo assumido:</b> <c>lower(name)</c> impede o uso do indice comum e forca
/// varredura da tabela. Com volume real, a correcao seria um indice funcional
/// (<c>CREATE INDEX ix_products_name_lower ON products (lower(name))</c>) ou busca
/// textual dedicada. Fica registrado aqui como decisao consciente, nao esquecimento.
/// </para>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Catalog.</param>
public sealed class ListProductsQueryHandler(ICatalogDbContext dbContext)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    /// <summary>
    /// Teto de itens por pagina.
    /// </summary>
    /// <remarks>
    /// Trava de seguranca: sem ela, um cliente pedindo <c>pageSize=1000000</c> derrubaria
    /// o servico. O valor pedido e sempre limitado a este maximo.
    /// </remarks>
    private const int MaxPageSize = 100;

    /// <inheritdoc />
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim().ToLowerInvariant()}%";
            query = query.Where(product => EF.Functions.Like(product.Name.ToLower(), term));
        }

        // A contagem roda antes da paginacao, sobre o mesmo filtro.
        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            // Ordenacao explicita e obrigatoria: sem ORDER BY, o Postgres nao garante
            // ordem estavel entre chamadas e o mesmo item poderia aparecer em duas
            // paginas diferentes (ou sumir de ambas).
            .OrderByDescending(product => product.CreatedAtUtc)
            .ThenBy(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.AvailableQuantity,
                product.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, page, pageSize, total);
    }
}
