using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.API.Grpc;
using Identity.API.Middleware;
using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "identity-service");

builder.Services.AddMassTransit(configuration =>
{
    configuration.ConfigureMarketplaceBus(builder.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<UserValidationGrpcService>();
app.MapGet("/", () => Results.Ok(new { service = "identity-service" }));

app.Run();
