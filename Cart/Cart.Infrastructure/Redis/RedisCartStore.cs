using System.Text.Json;
using Cart.Application.Abstractions;
using Cart.Domain.Entities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Cart.Infrastructure.Redis;

/// <summary>
/// Armazena o carrinho completo como um valor JSON no Redis.
/// </summary>
/// <remarks>
/// <para>
/// <b>String JSON e nao Hash do Redis.</b> O Hash permitiria atualizar um item isolado,
/// mas exigiria varios comandos por operacao e nao daria atomicidade ao carrinho como
/// um todo. Com uma unica chave por usuario, ler e gravar sao operacoes atomicas de um
/// comando so.
/// </para>
/// <para>
/// <b>TTL de 7 dias.</b> Carrinho abandonado nao pode ocupar memoria para sempre — e
/// memoria e o recurso caro do Redis. O TTL e renovado a cada gravacao, entao um
/// carrinho em uso nunca expira.
/// </para>
/// </remarks>
/// <param name="connectionMultiplexer">Conexao compartilhada com o Redis.</param>
/// <param name="logger">Logger usado para registrar payloads corrompidos.</param>
public sealed class RedisCartStore(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCartStore> logger) : ICartStore
{
    /// <summary>
    /// Tempo de vida de um carrinho sem alteracoes.
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    /// <inheritdoc />
    public async Task<ShoppingCart?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await connectionMultiplexer.GetDatabase().StringGetAsync(BuildKey(userId));

        if (!payload.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ShoppingCart>(payload!);
        }
        catch (JsonException exception)
        {
            // Payload gravado por uma versao antiga do modelo. Tratar como "carrinho
            // vazio" e melhor que estourar 500: o cliente monta o carrinho de novo, em
            // vez de ficar preso num erro que nenhuma acao dele resolve.
            logger.LogWarning(exception, "Carrinho invalido no Redis para o usuario {UserId}; sera descartado.", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await connectionMultiplexer.GetDatabase()
            .StringSetAsync(BuildKey(cart.UserId), JsonSerializer.Serialize(cart), Ttl);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await connectionMultiplexer.GetDatabase().KeyDeleteAsync(BuildKey(userId));
    }

    /// <summary>
    /// Monta a chave do carrinho no Redis.
    /// </summary>
    /// <remarks>
    /// O prefixo <c>cart:</c> segue a convencao de namespace por dois-pontos do Redis.
    /// Isso permite inspecionar (<c>SCAN MATCH cart:*</c>) e aplicar politicas por
    /// prefixo, alem de evitar colisao com as chaves de outros servicos que usam a mesma
    /// instancia.
    /// </remarks>
    private static string BuildKey(Guid userId) => $"cart:{userId}";
}
