using System.Text.Json;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Marketplace.Contracts.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Catalog.Infrastructure.Services;

/// <summary>
/// Leitura de produtos com estrategia <b>cache-aside</b> sobre Redis.
/// </summary>
/// <remarks>
/// <para>
/// <b>Como funciona o cache-aside (ou lazy loading):</b>
/// </para>
/// <code>
/// 1. procura no cache
///    ├─ achou (HIT)  → devolve, sem tocar no banco
///    └─ nao achou (MISS)
///          2. consulta o Postgres
///          3. grava no cache com TTL
///          4. devolve
/// </code>
/// <para>
/// <b>Por que este padrao, e nao write-through?</b> Porque so entra no cache o que
/// alguem realmente pediu. Num catalogo com 100 mil produtos onde 200 concentram o
/// trafego, o write-through encheria a memoria com 99,8% de itens que ninguem consulta.
/// </para>
/// <para>
/// <b>Os tres perigos classicos de cache — e o que este codigo faz sobre cada um:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Dado obsoleto.</b> Resolvido por <see cref="InvalidateAsync"/>, chamado em
///   toda escrita, mais o TTL como rede de seguranca. Era o bug da versao anterior:
///   havia cache, mas nenhuma invalidacao.</item>
///   <item><b>Cache stampede.</b> Se um item muito acessado expira, centenas de
///   requisicoes vao ao banco ao mesmo tempo. Mitigado aqui pelo TTL com jitter, que
///   evita que muitas chaves vencam no mesmo instante; a solucao completa exigiria um
///   lock distribuido.</item>
///   <item><b>Cache indisponivel.</b> Uma falha do Redis <b>nao</b> pode derrubar a
///   leitura. Por isso as operacoes de cache ficam em try/catch e caem para o banco —
///   o servico degrada em desempenho, nao em disponibilidade.</item>
/// </list>
/// </remarks>
/// <param name="dbContext">Contexto de leitura do catalogo.</param>
/// <param name="redis">Conexao com o Redis.</param>
/// <param name="logger">Logger usado para registrar falhas de cache.</param>
public sealed class CachedProductReadService(
    CatalogDbContext dbContext,
    IConnectionMultiplexer redis,
    ILogger<CachedProductReadService> logger) : IProductReadService
{
    /// <summary>
    /// Validade base das entradas de cache.
    /// </summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<ProductDetailsDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(productId);

        var cached = await TryGetFromCacheAsync(key);
        if (cached is not null)
        {
            return cached;
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .Where(candidate => candidate.Id == productId)
            .Select(candidate => new ProductDetailsDto(
                candidate.Id,
                candidate.Name,
                candidate.Price,
                candidate.AvailableQuantity))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is not null)
        {
            await TrySetCacheAsync(key, product);
        }

        // Ausencia NAO e cacheada de proposito. Guardar "nao existe" protegeria contra
        // cache penetration (consultas repetidas a ids inexistentes), mas exigiria
        // invalidar a marca negativa no cadastro do produto — complexidade que so se
        // justifica sob ataque real.
        return product;
    }

    /// <inheritdoc />
    public async Task InvalidateAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(BuildKey(productId));
        }
        catch (RedisException exception)
        {
            // Falhar a invalidacao e pior que falhar a leitura: o dado antigo sobrevive
            // ate o TTL vencer. Por isso este caso e logado como Warning, e nao ignorado.
            logger.LogWarning(exception, "Falha ao invalidar o cache do produto {ProductId}.", productId);
        }
    }

    private static string BuildKey(Guid productId) => $"catalog:product:{productId}";

    private async Task<ProductDetailsDto?> TryGetFromCacheAsync(string key)
    {
        try
        {
            var cached = await redis.GetDatabase().StringGetAsync(key);
            return cached.HasValue ? JsonSerializer.Deserialize<ProductDetailsDto>(cached!) : null;
        }
        catch (Exception exception) when (exception is RedisException or JsonException)
        {
            logger.LogWarning(exception, "Falha ao ler o cache de {Key}; consultando o banco.", key);
            return null;
        }
    }

    private async Task TrySetCacheAsync(string key, ProductDetailsDto product)
    {
        try
        {
            // Jitter de ate 60s: se mil produtos entrarem no cache no mesmo minuto (logo
            // apos um deploy, por exemplo), sem ele todos venceriam juntos e o banco
            // levaria a rajada inteira de uma vez.
            var ttl = CacheTtl + TimeSpan.FromSeconds(Random.Shared.Next(0, 60));

            await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(product), ttl);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Falha ao gravar o cache de {Key}.", key);
        }
    }
}
