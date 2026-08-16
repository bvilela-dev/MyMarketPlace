using System.Text;
using Marketplace.Infrastructure.Security;
using Marketplace.Infrastructure.Web.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Marketplace.Infrastructure.Web;

/// <summary>
/// Extensoes que padronizam a configuracao HTTP de todos os microsservicos.
/// </summary>
/// <remarks>
/// Cada <c>Program.cs</c> do projeto tinha o mesmo bloco de Swagger, autenticacao e
/// health check copiado. Concentrando aqui, uma melhoria de seguranca (por exemplo
/// exigir <c>ClockSkew = 0</c>) passa a valer para todos os servicos de uma vez.
/// </remarks>
public static class MarketplaceApiExtensions
{
    /// <summary>
    /// Nome da tag usada para separar as dependencias externas do check de readiness.
    /// </summary>
    public const string ReadinessTag = "ready";

    /// <summary>
    /// Registra a validacao de JWT Bearer usando a secao <c>"Jwt"</c> da configuracao.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chamado pelos servicos que apenas <i>consomem</i> o token (Cart, Order) e tambem
    /// pelo Identity, que alem disso o emite.
    /// </para>
    /// <para>
    /// Duas escolhas nao obvias aqui:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>ClockSkew = TimeSpan.Zero</c>: por padrao o .NET tolera <b>5 minutos</b>
    ///   de diferenca de relogio, ou seja, aceita um token ja expirado por ate 5 min.
    ///   Com access token de 15 minutos, isso e 30% de vida extra indevida.</item>
    ///   <item><c>MapInboundClaims = false</c>: impede a traducao automatica de <c>sub</c>
    ///   para a URI longa do WS-Security. Assim <c>User.FindFirst("sub")</c> funciona
    ///   exatamente como o padrao JWT descreve.</item>
    /// </list>
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <param name="configuration">Fonte de configuracao da aplicacao.</param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Lancada na inicializacao quando o segredo do JWT nao foi configurado ou e curto
    /// demais para HMAC-SHA256. Falhar aqui e proposital: e muito melhor o pod nao subir
    /// do que subir aceitando tokens assinados com uma chave fraca.
    /// </exception>
    public static IServiceCollection AddMarketplaceJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret ausente ou com menos de 32 bytes. Defina a variavel de ambiente Jwt__Secret com um segredo forte.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub"
                };
            });

        services.AddAuthorization();

        // ICurrentUser depende do HttpContext da requisicao em andamento.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    /// <summary>
    /// Registra o Swagger com suporte ao botao "Authorize" para JWT Bearer.
    /// </summary>
    /// <remarks>
    /// Sem a definicao de seguranca abaixo, o Swagger UI nao oferece campo para colar o
    /// token e todo endpoint protegido responde 401 na demonstracao — um tropeco comum
    /// em apresentacao ao vivo.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <param name="serviceTitle">Titulo exibido na pagina do Swagger.</param>
    /// <param name="includeXmlComments">
    /// Quando <see langword="true"/>, injeta os comentarios <c>///</c> do assembly na
    /// documentacao gerada — e o que faz a documentacao didatica aparecer no Swagger UI.
    /// </param>
    /// <returns>O proprio <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMarketplaceSwagger(this IServiceCollection services, string serviceTitle, bool includeXmlComments = true)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = serviceTitle,
                Version = "v1",
                Description = $"API do microsservico {serviceTitle} do MyMarketPlace."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Cole apenas o access token (o prefixo 'Bearer' e adicionado automaticamente)."
            });

            // Microsoft.OpenApi 3.x substituiu o antigo "OpenApiSecurityScheme com
            // Reference preenchido" por um tipo dedicado de referencia ($ref no JSON).
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            if (includeXmlComments)
            {
                var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name}.xml");
                if (File.Exists(xmlFile))
                {
                    options.IncludeXmlComments(xmlFile);
                }
            }
        });

        return services;
    }

    /// <summary>
    /// Registra os health checks basicos do servico.
    /// </summary>
    /// <remarks>
    /// A separacao entre <b>liveness</b> e <b>readiness</b> e o ponto central:
    /// <list type="bullet">
    ///   <item><b>liveness</b> ("o processo esta vivo?") nao consulta dependencia
    ///   nenhuma. Se falhar, o Kubernetes <b>reinicia</b> o pod.</item>
    ///   <item><b>readiness</b> ("posso receber trafego?") verifica banco e cache. Se
    ///   falhar, o pod so e <b>tirado do balanceador</b>, sem reinicio.</item>
    /// </list>
    /// Misturar os dois e um erro caro: uma queda momentanea do Postgres reiniciaria
    /// todos os pods em cascata, transformando uma indisponibilidade parcial numa queda
    /// total — exatamente quando o banco menos aguenta uma enxurrada de reconexoes.
    /// </remarks>
    /// <param name="services">Container de servicos.</param>
    /// <returns>Builder de health checks, para registrar dependencias adicionais.</returns>
    public static IHealthChecksBuilder AddMarketplaceHealthChecks(this IServiceCollection services)
        => services.AddHealthChecks();

    /// <summary>
    /// Adiciona a checagem de readiness do Redis.
    /// </summary>
    /// <param name="builder">Builder de health checks.</param>
    /// <returns>O proprio builder, para encadeamento.</returns>
    public static IHealthChecksBuilder AddRedisCheck(this IHealthChecksBuilder builder)
        => builder.AddCheck<RedisHealthCheck>("redis", tags: [ReadinessTag]);

    /// <summary>
    /// Adiciona a checagem de readiness de um banco relacional.
    /// </summary>
    /// <typeparam name="TDbContext">Contexto do EF Core a verificar.</typeparam>
    /// <param name="builder">Builder de health checks.</param>
    /// <param name="name">Nome exibido no relatorio de saude.</param>
    /// <returns>O proprio builder, para encadeamento.</returns>
    public static IHealthChecksBuilder AddDbContextCheck<TDbContext>(this IHealthChecksBuilder builder, string name = "postgres")
        where TDbContext : DbContext
        => builder.AddCheck<DbContextHealthCheck<TDbContext>>(name, tags: [ReadinessTag]);

    /// <summary>
    /// Publica os endpoints <c>/health/live</c> e <c>/health/ready</c>.
    /// </summary>
    /// <param name="endpoints">Builder de rotas da aplicacao.</param>
    /// <returns>O proprio builder, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapMarketplaceHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Predicate = _ => false descarta TODAS as checagens: o endpoint responde 200
        // pelo simples fato de o processo ter conseguido atender a requisicao.
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteHealthResponseAsync
        }).AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// Ativa o tratamento global de excecoes.
    /// </summary>
    /// <remarks>
    /// Deve ser o <b>primeiro</b> middleware registrado. O pipeline funciona como uma
    /// cebola: quem entra primeiro envolve todos os demais e, portanto, e o unico capaz
    /// de capturar excecoes lancadas la no fundo, no controller.
    /// </remarks>
    /// <param name="app">Builder do pipeline HTTP.</param>
    /// <returns>O proprio builder, para encadeamento.</returns>
    public static IApplicationBuilder UseMarketplaceExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    /// <summary>
    /// Serializa o relatorio de readiness em JSON legivel.
    /// </summary>
    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        });
    }
}
