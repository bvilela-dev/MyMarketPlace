using Identity.API.Grpc;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Marketplace.Infrastructure.Messaging;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using MassTransit;
using Microsoft.EntityFrameworkCore;

// ============================================================================
// Identity Service — autenticacao, perfil e enderecos do usuario.
//
// Responsabilidades:
//   * emitir e renovar tokens (JWT + refresh token com rotacao);
//   * guardar usuarios e enderecos;
//   * validar o par usuario/endereco por gRPC para o Order;
//   * anunciar novos usuarios via outbox (UserCreatedEvent).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) Registro de servicos (o "o que existe" no container).
// ---------------------------------------------------------------------------
builder.Services.AddApplication();                                       // MediatR + validadores + pipeline
builder.Services.AddInfrastructure(builder.Configuration);               // Postgres + seguranca + outbox
builder.Services.AddMarketplaceJwtAuthentication(builder.Configuration); // Emissao e validacao de token
builder.Services.AddMarketplaceTelemetry(builder.Configuration, "identity-service");

builder.Services.AddMassTransit(bus => bus.ConfigureMarketplaceBus(builder.Configuration, "identity"));

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddMarketplaceSwagger("Identity API");

builder.Services.AddMarketplaceHealthChecks()
    .AddDbContextCheck<IdentityDbContext>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// 2) Pipeline HTTP. A ORDEM AQUI E FUNCIONAL, nao estetica: cada middleware
//    envolve os seguintes, como camadas de uma cebola.
// ---------------------------------------------------------------------------

// Primeiro de todos: e o unico capaz de capturar excecoes de qualquer camada abaixo.
app.UseMarketplaceExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Autenticacao ANTES de autorizacao — nao da para decidir "pode?" sem antes
// responder "quem e?". Inverter faz [Authorize] rejeitar todo mundo com 401.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<UserValidationGrpcService>();
app.MapMarketplaceHealthEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "identity-service" })).AllowAnonymous();

// ---------------------------------------------------------------------------
// 3) Migracoes na inicializacao.
//
// Rodar migracao no startup e otimo para demonstracao e ambiente local, mas em
// producao tem um problema real: com N replicas subindo juntas, todas tentam
// migrar ao mesmo tempo. O caminho correto e um Job/initContainer do Kubernetes
// executando a migracao uma unica vez, antes do rollout dos pods.
// ---------------------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
