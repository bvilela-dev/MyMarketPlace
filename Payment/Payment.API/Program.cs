using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Payment.Application;
using Payment.Application.Consumers;
using Payment.Infrastructure;

// ============================================================================
// Payment Service — autorizacao (simulada) de pagamento.
//
// Servico puramente reativo: nao expoe endpoint de negocio, apenas consome
// OrderCreatedEvent e publica PaymentApprovedEvent ou PaymentFailedEvent.
//
// Continua sendo uma aplicacao web por dois motivos praticos: expor os health
// checks que o Kubernetes consulta e o endpoint de metricas.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "payment-service");

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<OrderCreatedConsumer>();
    bus.ConfigureMarketplaceBus(builder.Configuration, "payment");
});

builder.Services.AddMarketplaceHealthChecks().AddRedisCheck();

var app = builder.Build();

app.UseMarketplaceExceptionHandling();

app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "payment-service" }));

await app.RunAsync();
