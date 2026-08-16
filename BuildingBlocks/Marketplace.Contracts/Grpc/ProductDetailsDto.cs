namespace Marketplace.Contracts.Grpc;

/// <summary>
/// Dados de um produto devolvidos pelo Catalog via gRPC.
/// </summary>
/// <remarks>
/// Este DTO isola o restante do sistema das classes geradas pelo protobuf. Sem ele, o
/// tipo <c>GetProductResponse</c> gerado pelo compilador vazaria ate os handlers da
/// camada de aplicacao, que passariam a depender do formato do arquivo <c>.proto</c>.
/// </remarks>
/// <param name="ProductId">Identificador do produto.</param>
/// <param name="Name">Nome do produto.</param>
/// <param name="Price">Preco atual.</param>
/// <param name="AvailableQuantity">Quantidade disponivel em estoque.</param>
public sealed record ProductDetailsDto(Guid ProductId, string Name, decimal Price, int AvailableQuantity);
