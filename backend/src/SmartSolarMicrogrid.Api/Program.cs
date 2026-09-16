using SmartSolarMicrogrid.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddApiFoundation(builder.Configuration);
builder.Services.AddMongoDatabase(builder.Configuration);

var app = builder.Build();

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

app.MapControllers();
app.Run();

// Exposes the real application entry point to in-memory integration tests.
public partial class Program;
