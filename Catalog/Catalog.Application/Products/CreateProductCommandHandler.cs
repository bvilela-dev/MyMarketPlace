using System.Text.Json;
using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using MediatR;

namespace Catalog.Application.Products;

/// <summary>
/// Cadastra um produto no catalogo e anuncia o fato ao Inventory.
/// </summary>
/// <remarks>
/// <para>
/// Este handler amarra tres conceitos do projeto num caso de uso curto:
/// </para>
/// <list type="number">
///   <item><b>Modelo rico</b> — as regras de preco e quantidade vivem no construtor de
///   <see cref="Product"/>, nao aqui.</item>
///   <item><b>Outbox</b> — o <c>ProductCreatedEvent</c> entra na mesma transacao do
///   produto, garantindo que o Inventory sempre recebera o aviso.</item>
///   <item><b>Invalidacao de cache</b> — feita <i>depois</i> do commit. Invalidar antes
///   abriria uma janela em que outra requisicao repovoaria o cache com o dado antigo,
///   caso a transacao terminasse em rollback.</item>
/// </list>
/// </remarks>
/// <param name="dbContext">Contrato de persistencia do Catalog.</param>
/// <param name="productReadService">Servico de leitura, usado para invalidar o cache.</param>
public sealed class CreateProductCommandHandler(
    ICatalogDbContext dbContext,
    IProductReadService productReadService) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.Price,
            request.AvailableQuantity);

        await dbContext.Products.AddAsync(product, cancellationToken);

        await dbContext.OutboxMessages.AddAsync(
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = typeof(ProductCreatedEvent).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new ProductCreatedEvent(
                    Guid.NewGuid(),
                    product.Id,
                    product.Name,
                    product.Price,
                    product.AvailableQuantity,
                    product.CreatedAtUtc)),
                OccurredOnUtc = DateTime.UtcNow
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await productReadService.InvalidateAsync(product.Id, cancellationToken);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.AvailableQuantity,
            product.CreatedAtUtc);
    }
}
