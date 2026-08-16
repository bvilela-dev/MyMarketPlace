using Inventory.Application.Persistence;
using Inventory.Domain.Entities;
using Marketplace.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Persistencia das movimentacoes de estoque.
/// </summary>
/// <param name="dbContext">Contexto do banco do Inventory.</param>
public sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    /// <inheritdoc />
    public async Task EnsureStockAsync(Guid productId, int initialQuantity, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.StockItems.AnyAsync(item => item.ProductId == productId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.StockItems.Add(new StockItem(Guid.NewGuid(), productId, initialQuantity));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Corrida entre duas replicas processando o mesmo ProductCreatedEvent: as
            // duas passaram pelo AnyAsync e uma perdeu na disputa pelo indice unico.
            dbContext.ChangeTracker.Clear();

            // Se a linha existe, o resultado desejado ja foi alcancado pela outra
            // replica e a excecao pode ser absorvida. Caso contrario o erro e outro
            // (banco fora, constraint diferente) e deve subir para o retry da fila.
            if (!await StockExistsAsync(productId, cancellationToken))
            {
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task ReserveAsync(IReadOnlyDictionary<Guid, int> quantitiesByProduct, CancellationToken cancellationToken = default)
    {
        if (quantitiesByProduct.Count == 0)
        {
            return;
        }

        var productIds = quantitiesByProduct.Keys.ToArray();

        // Uma unica consulta para todos os produtos do pedido, em vez de N idas ao banco.
        // Ordenar por ProductId evita deadlock: duas transacoes que travem as mesmas
        // linhas em ordens opostas ficariam presas uma esperando a outra.
        var stockItems = await dbContext.StockItems
            .Where(item => productIds.Contains(item.ProductId))
            .OrderBy(item => item.ProductId)
            .ToListAsync(cancellationToken);

        var missing = productIds.Except(stockItems.Select(item => item.ProductId)).ToArray();
        if (missing.Length > 0)
        {
            throw new BusinessRuleException($"Produto sem cadastro de estoque: {string.Join(", ", missing)}.");
        }

        // Primeira passada: valida TODOS os itens antes de alterar qualquer um.
        // Sem isso, um pedido com 3 itens em que o ultimo falta poderia reservar os dois
        // primeiros e so entao lancar — deixando unidades presas indevidamente.
        foreach (var stockItem in stockItems)
        {
            var requested = quantitiesByProduct[stockItem.ProductId];

            if (stockItem.QuantityAvailable < requested)
            {
                throw new BusinessRuleException(
                    $"Estoque insuficiente para o produto {stockItem.ProductId}: disponivel {stockItem.QuantityAvailable}, solicitado {requested}.");
            }
        }

        // Segunda passada: aplica as reservas.
        foreach (var stockItem in stockItems)
        {
            stockItem.Reserve(quantitiesByProduct[stockItem.ProductId]);
        }

        try
        {
            // Um unico SaveChanges = uma unica transacao: ou todas as reservas do pedido
            // sao efetivadas, ou nenhuma.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concorrencia otimista via xmin (ver InventoryDbContext): outra transacao
            // alterou a mesma linha entre a leitura e a gravacao.
            //
            // Relancar como BusinessRuleException nao seria correto — nao e falta de
            // estoque, e uma disputa momentanea. Deixando a excecao subir, o retry do
            // MassTransit reprocessa a mensagem, que reler o saldo atualizado e decide
            // de novo. E exatamente para isso que o retry existe.
            throw;
        }
    }

    private Task<bool> StockExistsAsync(Guid productId, CancellationToken cancellationToken)
        => dbContext.StockItems.AsNoTracking().AnyAsync(item => item.ProductId == productId, cancellationToken);
}
