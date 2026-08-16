using Inventory.Application;
using Inventory.Application.Consumers;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;

// ============================================================================
// Inventory Service — dono da verdade sobre o estoque.
//
// Consome:
//   * ProductCreatedEvent  -> cria o saldo inicial do produto
//   * PaymentApprovedEvent -> reserva as unidades do pedido
//
// Publica:
//   * StockReservedEvent            (fluxo feliz)
//   * StockReservationFailedEvent   (compensacao: pago, mas sem estoque)
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "inventory-service");

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<ProductCreatedConsumer>();
    bus.AddConsumer<PaymentApprovedConsumer>();
    bus.ConfigureMarketplaceBus(builder.Configuration, "inventory");
});

builder.Services.AddMarketplaceHealthChecks()
    .AddDbContextCheck<InventoryDbContext>()
    .AddRedisCheck();

var app = builder.Build();

app.UseMarketplaceExceptionHandling();

app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "inventory-service" }));

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
