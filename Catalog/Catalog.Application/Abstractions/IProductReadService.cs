using Marketplace.Contracts.Grpc;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Leitura de produtos com cache.
/// </summary>
/// <remarks>
/// Separar a leitura da escrita e um passo em direcao a <b>CQRS</b>: a consulta pode ter
/// seu proprio caminho otimizado (aqui, cache-aside no Redis) sem que a escrita precise
/// conhece-lo.
/// </remarks>
public interface IProductReadService
{
    /// <summary>
    /// Busca um produto pelo identificador.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Dados do produto ou <see langword="null"/> quando inexistente.</returns>
    Task<ProductDetailsDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um produto do cache.
    /// </summary>
    /// <remarks>
    /// Chamado apos qualquer escrita. Sem esta invalidacao, uma alteracao de preco so
    /// apareceria depois de o TTL de 10 minutos expirar — e o Order continuaria criando
    /// pedidos com o preco antigo nesse intervalo.
    /// </remarks>
    /// <param name="productId">Identificador do produto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Task da operacao assincrona.</returns>
    Task InvalidateAsync(Guid productId, CancellationToken cancellationToken = default);
}
