using Marketplace.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketplaceTelemetry(builder.Configuration, "api-gateway");
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapReverseProxy();
app.MapGet("/", () => Results.Ok(new { service = "api-gateway" }));

app.Run();
