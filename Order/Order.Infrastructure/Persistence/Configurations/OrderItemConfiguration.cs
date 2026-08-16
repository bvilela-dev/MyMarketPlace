using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="OrderItem"/>.
/// </summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
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

        // Shadow property: a chave estrangeira existe no banco mas nao na entidade.
        // Manter OrderId fora do modelo reforca que OrderItem so e acessado atraves da
        // raiz do agregado — nao existe consulta "solta" de item de pedido.
        builder.Property<Guid>("OrderId");
        builder.HasIndex("OrderId");

        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(256).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();

        // Propriedade calculada em memoria: nao vira coluna.
        builder.Ignore(item => item.LineTotal);
    }
}
