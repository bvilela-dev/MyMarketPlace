using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

/// <summary>
/// Operacoes do Catalog necessarias ao Order.
/// </summary>
/// <remarks>
/// A interface expoe apenas o que o Order usa — nao o contrato gRPC inteiro. Isso
/// mantem os handlers livres das classes geradas pelo protobuf e torna trivial
/// substitui-la por um duble nos testes.
/// </remarks>
public interface ICatalogGrpcClient
{
    /// <summary>
    /// Busca os dados de um produto no Catalog.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Dados do produto.</returns>
    /// <exception cref="Marketplace.SharedKernel.Exceptions.NotFoundException">
    /// Lancada quando o produto nao existe no catalogo.
    /// </exception>
    Task<ProductDetailsDto> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
