using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="Product"/>.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);

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
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name).HasMaxLength(120).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(1024).IsRequired();

        // Precisao 18,2 = ate 16 digitos inteiros e 2 casas decimais. Sem isso o Npgsql
        // usaria numeric sem precisao definida, o que funciona mas nao documenta a
        // intencao nem protege contra valores fora de escala.
        builder.Property(product => product.Price).HasPrecision(18, 2).IsRequired();

        builder.Property(product => product.AvailableQuantity).IsRequired();
        builder.Property(product => product.CreatedAtUtc).IsRequired();

        // Indice que sustenta a ordenacao padrao da listagem paginada.
        builder.HasIndex(product => product.CreatedAtUtc);

        // Eventos de dominio sao estado em memoria, nunca coluna no banco.
        builder.Ignore(product => product.DomainEvents);
    }
}
