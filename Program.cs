using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebLynx.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services
builder.Services.AddSingleton<DiagnosticService>();
builder.Services.AddSingleton<DataLoggingService>();
builder.Services.AddSingleton<MessageParser>();
builder.Services.AddSingleton<RaceStateManager>();
builder.Services.AddSingleton<MultiPortTcpService>();
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
app.MapControllers();

// Configure the HTTP port
var httpPort = app.Configuration.GetValue<int>("HttpSettings:Port");
app.Urls.Add($"http://localhost:{httpPort}");

// Get the multi-port TCP service and start it
var multiPortTcpService = app.Services.GetRequiredService<MultiPortTcpService>();

// Start the TCP listeners
await multiPortTcpService.StartAsync();

// Log the HTTP service URL
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("HTTP API service will be available at: http://localhost:{Port}", httpPort);
logger.LogInformation("Swagger documentation will be available at: http://localhost:{Port}/swagger", httpPort);



// Keep the application running
await app.RunAsync();
