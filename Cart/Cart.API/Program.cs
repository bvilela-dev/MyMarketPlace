using Cart.Application;
using Cart.Infrastructure;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;

// ============================================================================
// Cart Service — carrinho de compras em Redis.
//
// O servico mais simples do conjunto, e de proposito: nao tem banco relacional,
// nao publica eventos e nao consome nenhum. Guarda um dado volatil e de alta
// frequencia de escrita, e a escolha de armazenamento reflete exatamente isso.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMarketplaceJwtAuthentication(builder.Configuration);
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "cart-service");

builder.Services.AddControllers();
builder.Services.AddMarketplaceSwagger("Cart API");

builder.Services.AddMarketplaceHealthChecks().AddRedisCheck();

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
app.MapGet("/", () => Results.Ok(new { service = "cart-service" })).AllowAnonymous();

await app.RunAsync();
