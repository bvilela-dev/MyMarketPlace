using System.Globalization;
using Catalog.API.Grpc;
using Grpc.Core;
using Marketplace.Contracts.Grpc;
using Marketplace.SharedKernel.Exceptions;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Grpc;

/// <summary>
/// Cliente gRPC do Catalog usado pelo Order.
/// </summary>
/// <remarks>
/// <para>
/// Faz a traducao entre o mundo do protobuf (<c>GetProductResponse</c>) e o do dominio
/// (<see cref="ProductDetailsDto"/>). Sem esta camada fina, os tipos gerados pelo
/// compilador do protobuf apareceriam dentro dos handlers.
/// </para>
/// <para>
/// <b>Tratamento de indisponibilidade:</b> uma <see cref="RpcException"/> com status
/// <c>Unavailable</c> significa que o Catalog esta fora do ar — e falha de
/// infraestrutura, nao erro de negocio. Convertida em <see cref="BusinessRuleException"/>
/// (HTTP 409) com mensagem clara, evita devolver 500 sem explicacao para o cliente.
/// </para>
/// </remarks>
/// <param name="client">Stub gRPC gerado a partir de <c>catalog.proto</c>.</param>
public sealed class CatalogGrpcClient(ProductCatalog.ProductCatalogClient client) : ICatalogGrpcClient
{
    /// <inheritdoc />
    public async Task<ProductDetailsDto> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        GetProductResponse response;

        try
        {
            response = await client.GetProductAsync(
                new GetProductRequest { ProductId = productId.ToString() },
                cancellationToken: cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new BusinessRuleException("Catalogo indisponivel no momento. Tente novamente em instantes.");
        }

        if (!response.Found)
        {
            throw new NotFoundException("Produto", productId);
        }

        // O preco trafega como string justamente para preservar a exatidao decimal.
        // InvariantCulture e obrigatorio: com cultura pt-BR, "349.90" seria lido como
        // trinta e quatro mil novecentos e noventa.
        var price = decimal.Parse(response.Price, NumberStyles.Number, CultureInfo.InvariantCulture);

        return new ProductDetailsDto(
            Guid.Parse(response.ProductId),
            response.Name,
            price,
            response.AvailableQuantity);
    }
}
