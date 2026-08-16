using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional do agregado <see cref="Order.Domain.Entities.Order"/>.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Domain.Entities.Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Domain.Entities.Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);

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
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.UserId).IsRequired();
        builder.Property(order => order.Total).HasPrecision(18, 2).IsRequired();

        // O enum e gravado como TEXTO, nao como int.
        //
        // Com int, a tabela guarda "2" — ilegivel numa consulta de suporte e, pior,
        // fragil: basta alguem inserir um valor novo no meio do enum para todos os
        // pedidos historicos mudarem de significado silenciosamente.
        // Como texto, o banco guarda "Confirmed" e reordenar o enum nao muda nada.
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(order => order.StatusReason).HasMaxLength(512);
        builder.Property(order => order.CreatedAtUtc).IsRequired();
        builder.Property(order => order.UpdatedAtUtc).IsRequired();

        // OwnsOne: o objeto de valor vira colunas da propria tabela de pedidos, sem
        // chave nem tabela separada. E o mapeamento correto para algo que nao tem
        // identidade propria e nunca e consultado isoladamente.
        builder.OwnsOne(order => order.AddressSnapshot, owned =>
        {
            owned.Property(snapshot => snapshot.Street).HasColumnName("street").HasMaxLength(256).IsRequired();
            owned.Property(snapshot => snapshot.Number).HasColumnName("number").HasMaxLength(32).IsRequired();
            owned.Property(snapshot => snapshot.City).HasColumnName("city").HasMaxLength(120).IsRequired();
            owned.Property(snapshot => snapshot.State).HasColumnName("state").HasMaxLength(120).IsRequired();
            owned.Property(snapshot => snapshot.ZipCode).HasColumnName("zip_code").HasMaxLength(32).IsRequired();
            owned.Property(snapshot => snapshot.Country).HasColumnName("country").HasMaxLength(120).IsRequired();
        });

        builder.Metadata.FindNavigation(nameof(Domain.Entities.Order.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(order => order.Items).WithOne().OnDelete(DeleteBehavior.Cascade);

        // Indice da consulta "meus pedidos", ordenada do mais recente para o mais antigo.
        builder.HasIndex(order => new { order.UserId, order.CreatedAtUtc });

        builder.Ignore(order => order.DomainEvents);
    }
}
