using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Notification.Application;
using Notification.Application.Consumers;
using Notification.Infrastructure;

// ============================================================================
// Notification Service — avisos ao cliente.
//
// Um consumidor por evento de integracao, cada um com sua propria fila:
//   user-created / payment-approved / payment-failed /
//   stock-reserved / stock-reservation-failed
//
// Filas separadas por consumidor significam que uma mensagem travada num tipo
// de notificacao nao impede o processamento dos demais.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "notification-service");

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<UserCreatedConsumer>();
    bus.AddConsumer<PaymentApprovedConsumer>();
    bus.AddConsumer<PaymentFailedConsumer>();
    bus.AddConsumer<StockReservedConsumer>();
    bus.AddConsumer<StockReservationFailedConsumer>();

    bus.ConfigureMarketplaceBus(builder.Configuration, "notification");
});

builder.Services.AddMarketplaceHealthChecks().AddRedisCheck();

var app = builder.Build();

app.UseMarketplaceExceptionHandling();

app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "notification-service" }));

await app.RunAsync();
