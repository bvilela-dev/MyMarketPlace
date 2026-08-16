namespace Catalog.Application.Products;

/// <summary>
/// Produto exposto pela API REST do catalogo.
/// </summary>
/// <param name="Id">Identificador do produto.</param>
/// <param name="Name">Nome.</param>
/// <param name="Description">Descricao.</param>
/// <param name="Price">Preco de tabela.</param>
/// <param name="AvailableQuantity">Quantidade disponivel na vitrine.</param>
/// <param name="CreatedAtUtc">Momento (UTC) do cadastro.</param>
public sealed record ProductDto(Guid Id, string Name, string Description, decimal Price, int AvailableQuantity, DateTime CreatedAtUtc);

/// <summary>
/// Pagina de resultados de uma consulta.
/// </summary>
/// <remarks>
/// <b>Por que paginar sempre?</b> Um <c>GET /api/products</c> sem limite parece
/// inofensivo com 10 produtos de demonstracao e derruba o servico com 500 mil. Devolver
/// <paramref name="Total"/> junto permite ao cliente montar a navegacao sem uma segunda
/// chamada so para contar.
/// </remarks>
/// <typeparam name="T">Tipo dos itens da pagina.</typeparam>
/// <param name="Items">Itens da pagina atual.</param>
/// <param name="Page">Numero da pagina (base 1).</param>
/// <param name="PageSize">Tamanho da pagina.</param>
/// <param name="Total">Total de registros que atendem ao filtro.</param>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long Total)
{
    /// <summary>
    /// Quantidade total de paginas disponiveis.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
