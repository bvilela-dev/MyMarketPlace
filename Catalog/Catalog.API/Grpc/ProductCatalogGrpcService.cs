using Catalog.Application.Abstractions;
using Grpc.Core;

namespace Catalog.API.Grpc;

/// <summary>
/// Servico gRPC de consulta de produtos, usado pelo Order.
/// </summary>
/// <remarks>
/// <para>
/// Reaproveita o mesmo <see cref="IProductReadService"/> da API REST — logo, tambem se
/// beneficia do cache. Duplicar a consulta aqui criaria duas fontes de verdade para a
/// mesma leitura.
/// </para>
/// <para>
/// <b>Correcao aplicada:</b> <c>Guid.Parse</c> foi trocado por <c>TryParse</c>. Um id
/// malformado devolvia <c>FormatException</c> como erro interno de gRPC, e o Order
/// (com retry do Polly) ainda repetia a chamada tres vezes antes de falhar.
/// </para>
/// </remarks>
/// <param name="productReadService">Servico de leitura de produtos.</param>
public sealed class ProductCatalogGrpcService(IProductReadService productReadService) : ProductCatalog.ProductCatalogBase
{
    /// <summary>
    /// Retorna os dados de um produto.
    /// </summary>
    /// <param name="request">Identificador do produto solicitado.</param>
    /// <param name="context">Contexto da chamada gRPC.</param>
    /// <returns>Dados do produto, ou <c>Found = false</c> quando inexistente.</returns>
    public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var productId))
        {
            return new GetProductResponse { Found = false };
        }

        var product = await productReadService.GetByIdAsync(productId, context.CancellationToken);

        if (product is null)
        {
            return new GetProductResponse { Found = false };
        }

        return new GetProductResponse
        {
            Found = true,
            ProductId = product.ProductId.ToString(),
            // Protobuf nao tem tipo decimal nativo; o preco trafega como string e e
            // reconvertido para decimal do outro lado, preservando os centavos.
            // Usar 'double' aqui (como na versao anterior) introduziria erro de
            // arredondamento justamente em valores monetarios.
            Price = product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Name = product.Name,
            AvailableQuantity = product.AvailableQuantity
        };
    }
}
