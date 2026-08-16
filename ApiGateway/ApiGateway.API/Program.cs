using System.Threading.RateLimiting;
using Marketplace.Infrastructure.Observability;
using Marketplace.Infrastructure.Web;
using Microsoft.AspNetCore.RateLimiting;

// ============================================================================
// API Gateway — porta unica de entrada do marketplace (YARP).
//
// Por que existe:
//   * o cliente conhece UM endereco, nao sete (e nao quebra quando um servico
//     muda de host ou e dividido em dois);
//   * elimina o problema de CORS entre origens diferentes;
//   * concentra o que e transversal — rate limiting, logging de borda e, em um
//     sistema real, tambem TLS e WAF.
//
// O que o gateway NAO faz aqui, de proposito: regra de negocio. Gateway que
// decide preco ou monta pedido vira o novo monolito, agora sem testes.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceTelemetry(builder.Configuration, "api-gateway");

// As rotas e clusters vem inteiramente da configuracao (appsettings + variaveis de
// ambiente). Em Kubernetes, apontar para outro destino e so trocar um ConfigMap.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ---------------------------------------------------------------------------
// Rate limiting na borda.
//
// Particionado por usuario autenticado quando ha token, e por IP quando nao ha.
// So por IP nao bastaria: usuarios atras do mesmo NAT corporativo dividiriam a
// mesma cota e um derrubaria o outro.
//
// Algoritmo: fixed window. Simples e previsivel; o custo conhecido e o efeito de
// borda (ate 2x o limite na virada da janela). Sliding window ou token bucket
// resolvem isso ao preco de mais estado.
//
// Limitacao honesta: o contador vive na memoria do pod. Com 3 replicas, o limite
// efetivo e 3x o configurado. Um limite realmente global exigiria contador
// compartilhado no Redis.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.FindFirst("sub")?.Value}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100),
            Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60)),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

builder.Services.AddMarketplaceHealthChecks();

var app = builder.Build();

app.UseMarketplaceExceptionHandling();
app.UseRateLimiter();

app.MapReverseProxy();
app.MapMarketplaceHealthEndpoints();

// Pagina inicial com o mapa de rotas: e a primeira coisa que se abre numa
// demonstracao, e evita ter de decorar os prefixos.
app.MapGet("/", () => Results.Ok(new
{
    service = "api-gateway",
    routes = new[]
    {
        "/identity/api/auth/register",
        "/identity/api/auth/login",
        "/identity/api/auth/refresh",
        "/identity/api/users/me",
        "/catalog/api/products",
        "/cart/api/carts/me",
        "/order/api/orders"
    }
}));

await app.RunAsync();
