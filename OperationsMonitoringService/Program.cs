using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OperationsMonitoringService.Data;
using OperationsMonitoringService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TelemetryIngestionService>();
builder.Services.AddOptions<MonitoringOptions>()
    .BindConfiguration(MonitoringOptions.SectionName)
    .Validate(options => options.FeverThresholdCelsius is >= 35m and <= 45m,
        "The fever threshold must be between 35 and 45 °C.")
    .ValidateOnStart();

var postgresConnection = builder.Configuration.GetConnectionString("Monitoring");
var useDemoDatabase = builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Database:UseInMemory");
if (!useDemoDatabase && string.IsNullOrWhiteSpace(postgresConnection))
    throw new InvalidOperationException("ConnectionStrings:Monitoring must be configured.");
builder.Services.AddDbContext<MonitoringDbContext>(options =>
{
    if (useDemoDatabase)
        options.UseInMemoryDatabase("monitoring-development-demo");
    else
        options.UseNpgsql(postgresConnection);
});

var app = builder.Build();

// The local demo exposes Swagger. Outside Development a configured API key is mandatory
// until the Identity & Access Management service provides user-scoped authorization.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.ConfigObject.DeepLinking = true);
}
else
{
    var configuredKey = app.Configuration["Security:ApiKey"];
    if (string.IsNullOrWhiteSpace(configuredKey))
        throw new InvalidOperationException("Security:ApiKey must be configured outside Development.");

    var expectedKey = Encoding.UTF8.GetBytes(configuredKey);
    app.Use(async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next();
            return;
        }

        var suppliedKey = context.Request.Headers["X-API-Key"].ToString();
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);
        if (suppliedBytes.Length != expectedKey.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next();
    });
}

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    await database.Database.EnsureCreatedAsync();
}

app.MapGet("/health", async (MonitoringDbContext database, CancellationToken cancellationToken) =>
    await database.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "Healthy" })
        : Results.Problem("The monitoring database is unavailable.", statusCode: 503));

app.MapControllers();
app.Run();

public partial class Program;
