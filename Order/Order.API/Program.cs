using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "order-service");
builder.Services.AddMassTransit(configuration => configuration.ConfigureMarketplaceBus(builder.Configuration));
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
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "order-service" }));

app.Run();
