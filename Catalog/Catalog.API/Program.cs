using Catalog.API.Grpc;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;

// ============================================================================
// Catalog Service — vitrine de produtos.
//
// Responsabilidades:
//   * expor produtos por REST (listagem paginada e consulta por id);
//   * responder consultas de produto por gRPC para o Order;
//   * usar Redis como cache-aside na leitura por id;
//   * anunciar produtos novos via outbox (ProductCreatedEvent -> Inventory).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceJwtAuthentication(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "catalog-service");

builder.Services.AddMassTransit(bus => bus.ConfigureMarketplaceBus(builder.Configuration, "catalog"));

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddMarketplaceSwagger("Catalog API");

builder.Services.AddMarketplaceHealthChecks()
    .AddDbContextCheck<CatalogDbContext>()
    .AddRedisCheck();

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
app.MapGrpcService<ProductCatalogGrpcService>();
app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "catalog-service" })).AllowAnonymous();

// ---------------------------------------------------------------------------
// Migracao + seed de demonstracao.
//
// O seed roda apenas quando "Seed:Enabled" e true (padrao em Development e no
// docker-compose). Em producao a flag fica desligada: dados de demonstracao
// jamais devem aparecer no catalogo real.
// ---------------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.MigrateAsync();

    if (builder.Configuration.GetValue("Seed:Enabled", app.Environment.IsDevelopment()))
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CatalogSeeder");
        await CatalogSeeder.SeedAsync(dbContext, logger);
    }
}

await app.RunAsync();
