using Inventory.Application;
using Inventory.Application.Consumers;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "inventory-service");
builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumer<PaymentApprovedConsumer>();
    configuration.ConfigureMarketplaceBus(builder.Configuration);
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "inventory-service" }));

app.Run();
