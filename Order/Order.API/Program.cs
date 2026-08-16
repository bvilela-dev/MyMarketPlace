using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application;
using Order.Application.Consumers;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;

// ============================================================================
// Order Service — criacao e acompanhamento de pedidos.
//
// E o servico mais completo do projeto: ao mesmo tempo
//   * PRODUTOR  — publica OrderCreatedEvent via outbox;
//   * CONSUMIDOR — reage a pagamento e estoque para mover o status do pedido;
//   * CLIENTE gRPC — consulta Identity e Catalog de forma sincrona e resiliente.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceJwtAuthentication(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "order-service");

builder.Services.AddMassTransit(bus =>
{
    // Cada consumidor ganha a sua propria fila (order-payment-approved,
    // order-stock-reserved...). Filas separadas significam que uma mensagem travada
    // num consumidor nao bloqueia os demais.
    bus.AddConsumer<PaymentApprovedConsumer>();
    bus.AddConsumer<PaymentFailedConsumer>();
    bus.AddConsumer<StockReservedConsumer>();
    bus.AddConsumer<StockReservationFailedConsumer>();

    bus.ConfigureMarketplaceBus(builder.Configuration, "order");
});

builder.Services.AddControllers();
builder.Services.AddMarketplaceSwagger("Order API");

builder.Services.AddMarketplaceHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

var app = builder.Build();

app.UseMarketplaceExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "order-service" })).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
