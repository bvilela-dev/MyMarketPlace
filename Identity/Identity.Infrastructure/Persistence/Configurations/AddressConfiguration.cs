using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="Address"/>.
/// </summary>
public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(address => address.Id);

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
        builder.Property(address => address.Id).ValueGeneratedNever();

        builder.Property(address => address.Street).HasMaxLength(256).IsRequired();
        builder.Property(address => address.Number).HasMaxLength(32).IsRequired();
        builder.Property(address => address.City).HasMaxLength(120).IsRequired();
        builder.Property(address => address.State).HasMaxLength(120).IsRequired();
        builder.Property(address => address.ZipCode).HasMaxLength(32).IsRequired();
        builder.Property(address => address.Country).HasMaxLength(120).IsRequired();

        // Toda consulta de endereco parte do usuario dono.
        builder.HasIndex(address => address.UserId);
    }
}
