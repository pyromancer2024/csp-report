using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// ✅ Allow all origins (TESTING ONLY)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllForCspReports", policy =>
    {
        policy
            .AllowAnyOrigin()
            .WithMethods("POST", "OPTIONS")
            .WithHeaders("content-type");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ✅ Enable CORS middleware
app.UseCors("AllowAllForCspReports");

// ✅ Handle BOTH OPTIONS + POST for CSP reporting
app.MapMethods("/csp-report", new[] { "POST", "OPTIONS" }, async (HttpContext ctx, ILogger<Program> log) =>
{
    if (ctx.Request.Method == "OPTIONS")
        return Results.NoContent();

    using var reader = new StreamReader(ctx.Request.Body);
    var raw = await reader.ReadToEndAsync();

    var truncated = raw.Length > 20_000 ? raw[..20_000] + "…(truncated)" : raw;

    log.LogWarning(
        "CSP Report received. ContentType={ContentType}, Host={Host}, UA={UA}, IP={IP}, Body={Body}",
        ctx.Request.ContentType,
        ctx.Request.Host.Value,
        ctx.Request.Headers.UserAgent.ToString(),
        ctx.Connection.RemoteIpAddress?.ToString(),
        truncated
    );

    // Best-effort parse (don’t fail endpoint)
    try { _ = JsonDocument.Parse(raw); } catch { }

    return Results.NoContent();
})
.Accepts<string>("application/csp-report", "application/reports+json", "application/json")
.Produces(StatusCodes.Status204NoContent);

app.MapGet("/health", () => Results.Ok("ok"));

// Note: HTTPS binding requires a trusted cert (and ideally matching the host/IP)
app.Run("https://0.0.0.0:5100");
