using System.Text.Json;
using Catalog.Domain.Entities;
using Marketplace.Contracts.Events;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Popula o catalogo com produtos de demonstracao na primeira execucao.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que existe.</b> Sem produtos cadastrados o sistema nao demonstra nada: nao ha
/// o que colocar no carrinho, nem pedido a criar, e a coreografia inteira
/// (pagamento → estoque → notificacao) fica inacessivel. O seed transforma um
/// <c>docker compose up</c> num ambiente ja navegavel.
/// </para>
/// <para>
/// <b>Idempotente.</b> Se ja existir qualquer produto, o seed nao faz nada. Isso permite
/// rodar na inicializacao de todas as replicas sem duplicar dados.
/// </para>
/// <para>
/// <b>Passa pelo mesmo caminho da API.</b> Os produtos entram junto com seus
/// <c>ProductCreatedEvent</c> no outbox — exatamente como aconteceria via
/// <c>POST /api/products</c>. E assim que o Inventory recebe o estoque inicial, sem
/// precisar de um segundo script de seed sincronizado a mao.
/// </para>
/// </remarks>
public static class CatalogSeeder
{
    /// <summary>
    /// Catalogo de demonstracao.
    /// </summary>
    private static readonly (string Name, string Description, decimal Price, int Quantity)[] DemoProducts =
    [
        ("Teclado Mecanico RGB", "Teclado mecanico com switches lineares e iluminacao RGB por tecla.", 349.90m, 50),
        ("Mouse Sem Fio Ergonomico", "Mouse vertical sem fio com sensor de 16.000 DPI.", 219.90m, 80),
        ("Monitor 27\" QHD 165Hz", "Monitor IPS de 27 polegadas, 2560x1440, 165Hz e 1ms.", 1899.00m, 25),
        ("Headset Gamer 7.1", "Headset com som surround 7.1 virtual e microfone com cancelamento de ruido.", 459.00m, 40),
        ("SSD NVMe 1TB", "SSD NVMe PCIe 4.0 com leitura de ate 7.000 MB/s.", 649.90m, 60),
        ("Webcam Full HD 60fps", "Webcam 1080p a 60fps com foco automatico e correcao de luz.", 389.00m, 35),
        ("Cadeira Ergonomica", "Cadeira de escritorio com apoio lombar ajustavel e encosto em tela.", 1299.00m, 15),
        ("Hub USB-C 8 em 1", "Hub USB-C com HDMI 4K, leitor de cartoes e entrega de energia de 100W.", 279.90m, 70)
    ];

    /// <summary>
    /// Insere os produtos de demonstracao caso o catalogo esteja vazio.
    /// </summary>
    /// <param name="dbContext">Contexto do catalogo.</param>
    /// <param name="logger">Logger usado para registrar o resultado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Task da operacao assincrona.</returns>
    public static async Task SeedAsync(CatalogDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Catalogo ja populado; seed ignorado.");
            return;
        }

        foreach (var (name, description, price, quantity) in DemoProducts)
        {
            var product = new Product(Guid.NewGuid(), name, description, price, quantity);

            dbContext.Products.Add(product);
            dbContext.OutboxMessages.Add(new OutboxMessage
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
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Catalogo populado com {Count} produtos de demonstracao.", DemoProducts.Length);
    }
}
