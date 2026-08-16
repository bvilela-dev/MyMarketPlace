using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="RefreshToken"/>.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);

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
        builder.Property(token => token.Id).ValueGeneratedNever();

        // Hash SHA-256 em Base64 tem sempre 44 caracteres.
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();

        // Indice unico no hash: alem de impedir colisao, e ele que torna a busca do
        // refresh token uma operacao O(log n) em vez de varredura completa da tabela.
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.Property(token => token.CreatedAtUtc).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).IsRequired();
        builder.Property(token => token.IsRevoked).IsRequired();

        // Indice de apoio a rotina de limpeza de tokens vencidos.
        builder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
    }
}
