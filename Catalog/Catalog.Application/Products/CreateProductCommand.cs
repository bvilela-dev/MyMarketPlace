using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Comando de cadastro de um produto.
/// </summary>
/// <param name="Name">Nome do produto.</param>
/// <param name="Description">Descricao do produto.</param>
/// <param name="Price">Preco de tabela.</param>
/// <param name="AvailableQuantity">Quantidade inicial de estoque.</param>
public sealed record CreateProductCommand(string Name, string Description, decimal Price, int AvailableQuantity)
    : IRequest<ProductDto>;
