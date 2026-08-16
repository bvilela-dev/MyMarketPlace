using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence;

/// <summary>
/// Mapeamento relacional compartilhado da tabela de outbox.
/// </summary>
/// <remarks>
/// Identity e Order tinham cada um a sua propria <c>OutboxMessageConfiguration</c>, com
/// diferencas sutis (uma tinha <c>IsRequired</c> em <c>OccurredOnUtc</c>, a outra nao).
/// Como a tabela e lida por um unico componente compartilhado — o
/// <c>OutboxPublisherBackgroundService</c> —, o mapeamento tambem precisa ser unico.
/// </remarks>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Aplica o mapeamento da tabela <c>outbox_messages</c> ao modelo.
    /// </summary>
    /// <remarks>
    /// Chamada explicitamente no <c>OnModelCreating</c> de cada contexto, porque
    /// <c>ApplyConfigurationsFromAssembly</c> so enxerga configuracoes do proprio
    /// assembly do servico — nunca as de um building block.
    /// </remarks>
    /// <param name="modelBuilder">Construtor do modelo do EF Core.</param>
    /// <returns>O proprio <paramref name="modelBuilder"/>, para encadeamento.</returns>
    public static ModelBuilder ApplyOutboxConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(message => message.Id);
            // Id gerado pela aplicacao (ver nota em UserConfiguration).
            builder.Property(message => message.Id).ValueGeneratedNever();

            builder.Property(message => message.Type).HasMaxLength(512).IsRequired();

            // jsonb (e nao text): o Postgres armazena de forma binaria e indexavel,
            // permitindo consultar dentro do payload durante uma investigacao —
            // ex.: "quais eventos de outbox mencionam este OrderId?".
            builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();

            builder.Property(message => message.OccurredOnUtc).IsRequired();
            builder.Property(message => message.AttemptCount).IsRequired().HasDefaultValue(0);
            builder.Property(message => message.Error).HasMaxLength(2048);

            // Indice que sustenta a consulta do publicador, executada a cada 5 segundos:
            // "pendentes (ProcessedOnUtc == null), mais antigas primeiro".
            // O filtro parcial mantem o indice pequeno: linhas ja publicadas — a imensa
            // maioria da tabela com o tempo — simplesmente nao entram nele.
            builder
                .HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc })
                .HasFilter("\"ProcessedOnUtc\" IS NULL")
                .HasDatabaseName("ix_outbox_messages_pending");
        });

        return modelBuilder;
    }
}
