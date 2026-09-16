using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Services;

var bootstrap = args.Contains("--bootstrap-backoffice");
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--bootstrap-backoffice").ToArray());
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddApiFoundation(builder.Configuration);
builder.Services.AddMongoDatabase(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddStationManagement();
builder.Services.AddSlotManagement();
builder.Services.AddReservationManagement();
builder.Services.AddQrTransactions();

var app = builder.Build();

if (bootstrap)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Backoffice bootstrap is available only in Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetServices<IHostedService>().OfType<MongoDatabaseInitializer>().Single().StartAsync(CancellationToken.None);
    await scope.ServiceProvider.GetRequiredService<BackofficeBootstrapService>().CreateAsync(CancellationToken.None);
    app.Logger.LogInformation("Initial Backoffice account created. Remove Bootstrap secrets after use.");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors(ApiServiceRegistration.BrowserCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "Smart Solar Microgrid API v1");
        options.DocumentTitle = "Smart Solar Microgrid API";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers().RequireAuthorization();
app.Run();

// Exposes the real application entry point to in-memory integration tests.
public partial class Program;
