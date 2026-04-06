using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using MassTransit;
using Notification.Application;
using Notification.Application.Consumers;
using Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "notification-service");
builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumer<UserCreatedConsumer>();
    configuration.AddConsumer<PaymentApprovedConsumer>();
    configuration.AddConsumer<PaymentFailedConsumer>();
    configuration.AddConsumer<StockReservedConsumer>();
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
app.MapGet("/", () => Results.Ok(new { service = "notification-service" }));

app.Run();
