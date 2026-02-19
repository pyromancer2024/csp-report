using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/csp-report", async (HttpRequest req, ILogger<Program> log) =>
{
    using var reader = new StreamReader(req.Body);
    var raw = await reader.ReadToEndAsync();

    var truncated = raw.Length > 20_000 ? raw[..20_000] + "…(truncated)" : raw;

    log.LogWarning(
        "CSP Report received. ContentType={ContentType}, Host={Host}, UA={UA}, IP={IP}, Body={Body}",
        req.ContentType,
        req.Host.Value,
        req.Headers.UserAgent.ToString(),
        req.HttpContext.Connection.RemoteIpAddress?.ToString(),
        truncated
    );

    // Best-effort parse (don’t fail endpoint)
    try
    {
        _ = JsonDocument.Parse(raw);
    }
    catch (JsonException ex)
    {

    }

    return Results.NoContent();
})
.Accepts<string>("application/csp-report", "application/reports+json", "application/json")
.Produces(StatusCodes.Status204NoContent);

app.MapGet("/health", () => Results.Ok("ok"));

app.Run("https://0.0.0.0:5100");
