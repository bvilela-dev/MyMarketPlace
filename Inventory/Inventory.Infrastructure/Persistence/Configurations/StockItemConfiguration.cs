using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="StockItem"/>.
/// </summary>
public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(item => item.Id);

        // ValueGeneratedNever: o identificador e gerado pela APLICACAO (Guid.NewGuid no
        // construtor da entidade), nunca pelo banco.
        //
        // Sem esta linha o EF Core assume a convencao "chave Guid = gerada no INSERT" e,
        // ao encontrar um filho novo com Id ja preenchido dentro da colecao de um pai
        // rastreado, conclui que a linha JA EXISTE — marcando-a como Modified em vez de
        // Added. O resultado e um UPDATE em algo que nunca foi inserido, e a excecao
        // "Attempted to update or delete an entity that does not exist in the store".
        //
        // Gerar o Id na aplicacao e proposital: o agregado precisa conhecer a propria
        // identidade antes de tocar no banco (para referenciar em eventos, por exemplo).
        builder.Property(item => item.Id).ValueGeneratedNever();

        // Um unico saldo por produto. Este indice tambem e o que resolve a corrida entre
        // duas replicas processando o mesmo ProductCreatedEvent ao mesmo tempo.
        builder.HasIndex(item => item.ProductId).IsUnique();

        builder.Property(item => item.QuantityAvailable).IsRequired();
        builder.Property(item => item.QuantityReserved).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();

        // Propriedade calculada: nao vira coluna.
        builder.Ignore(item => item.QuantityOnHand);

        // ---------------------------------------------------------------------
        // CONCORRENCIA OTIMISTA via xmin
        //
        // xmin e uma coluna de sistema do Postgres que guarda o id da transacao que
        // gravou a linha pela ultima vez — ou seja, um numero de versao gratuito.
        //
        // Com isso, o UPDATE gerado pelo EF vira:
        //     UPDATE stock_items SET ... WHERE id = @id AND xmin = @xmin_lido
        // Se outra transacao alterou a linha nesse meio-tempo, o xmin mudou, zero linhas
        // sao afetadas e o EF lanca DbUpdateConcurrencyException.
        //
        // Por que otimista e nao pessimista (SELECT FOR UPDATE)? Porque conflito real e
        // raro: e improvavel que dois pedidos do mesmo produto colidam no mesmo
        // milissegundo. Travar toda leitura para proteger um caso raro custaria
        // throughput em 100% das operacoes.
        //
        // SEM isto, duas reservas simultaneas do ultimo item leriam ambas
        // "disponivel = 1" e ambas gravariam "0" — vendendo o mesmo produto duas vezes.
        // ---------------------------------------------------------------------
        // Shadow property mapeada para a coluna de sistema "xmin", marcada como
        // token de versao. (Substitui o antigo helper UseXminAsConcurrencyToken,
        // removido nas versoes recentes do provider Npgsql.)
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
    }
}
