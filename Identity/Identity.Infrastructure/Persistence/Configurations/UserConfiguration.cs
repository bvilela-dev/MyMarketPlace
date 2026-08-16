using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="User"/>.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

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
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Name).HasMaxLength(120).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();

        // Ultima linha de defesa contra e-mail duplicado. A checagem no handler cobre o
        // caso comum com uma mensagem amigavel; este indice cobre a corrida entre duas
        // requisicoes simultaneas, que nenhuma checagem em codigo consegue evitar.
        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(user => user.CreatedAtUtc).IsRequired();

        // Cascade: apagar o usuario apaga enderecos e tokens. Faz sentido porque nenhum
        // dos dois existe fora do agregado do usuario.
        builder.HasMany(user => user.Addresses)
            .WithOne()
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // As colecoes sao expostas como IReadOnlyCollection sobre campos privados.
        // Sem estas duas linhas, o EF tentaria escrever pela propriedade (que nao tem
        // setter) e falharia ao materializar a entidade vinda do banco.
        builder.Metadata.FindNavigation(nameof(User.Addresses))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
