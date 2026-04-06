using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using MassTransit;
using Payment.Application;
using Payment.Application.Consumers;
using Payment.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "payment-service");
builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumer<OrderCreatedConsumer>();
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

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "payment-service" }));

app.Run();
