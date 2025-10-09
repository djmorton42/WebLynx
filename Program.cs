using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebLynx.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configure broadcast settings
builder.Services.Configure<WebLynx.Models.BroadcastSettings>(
    builder.Configuration.GetSection("BroadcastSettings"));

// Configure lap counter settings
builder.Services.Configure<WebLynx.Models.LapCounterSettings>(
    builder.Configuration.GetSection("LapCounterSettings"));

// Configure logging settings
builder.Services.Configure<WebLynx.Models.LoggingSettings>(
    builder.Configuration.GetSection("LoggingSettings"));

// Configure view properties
builder.Services.Configure<WebLynx.Models.ViewProperties>(
    builder.Configuration.GetSection("ViewProperties"));

// Configure host options for faster shutdown
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(5);
});

// Add services
builder.Services.AddSingleton<DiagnosticService>();
builder.Services.AddSingleton<DataLoggingService>();
builder.Services.AddSingleton<MessageParser>();
builder.Services.AddSingleton<RaceStateManager>();
builder.Services.AddSingleton<MultiPortTcpService>();
builder.Services.AddSingleton<TemplateService>();
builder.Services.AddSingleton<ViewDiscoveryService>();
builder.Services.AddHostedService<LiveRaceFileWriter>();

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Map controllers first to handle dynamic view routing
app.MapControllers();

// Serve static files from Views directory (for CSS, images, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Views")),
    RequestPath = "/views",
    ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
    {
        Mappings = { [".avif"] = "image/avif" }
    }
});

// Serve other static files (like favicon.ico) from wwwroot
app.UseStaticFiles();

// Configure the HTTP port
var httpPort = app.Configuration.GetValue<int>("HttpSettings:Port");
app.Urls.Add($"http://0.0.0.0:{httpPort}");

// Get the multi-port TCP service and start it
var multiPortTcpService = app.Services.GetRequiredService<MultiPortTcpService>();

// Initialize view discovery
var viewDiscoveryService = app.Services.GetRequiredService<ViewDiscoveryService>();
viewDiscoveryService.DiscoverViews();

// Start the TCP listeners
await multiPortTcpService.StartAsync();

// Log the HTTP service URL
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("HTTP API service will be available at: http://localhost:{Port}", httpPort);
logger.LogInformation("Swagger documentation will be available at: http://localhost:{Port}/swagger", httpPort);

// Register graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Application is shutting down, stopping TCP listeners...");
    multiPortTcpService.Stop();
    logger.LogInformation("TCP listeners stopped");
});

// Keep the application running
await app.RunAsync();
