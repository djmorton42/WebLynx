using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace WebLynx.Services;

public class MultiPortTcpService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MultiPortTcpService> _logger;
    private readonly DataLoggingService _dataLoggingService;
    private readonly DiagnosticService _diagnosticService;
    private readonly RaceStateManager _raceStateManager;
    private TcpListener? _timingListener;
    private TcpListener? _resultsListener;
    private CancellationTokenSource? _cancellationTokenSource;

    public MultiPortTcpService(IConfiguration configuration, ILogger<MultiPortTcpService> logger, DataLoggingService dataLoggingService, DiagnosticService diagnosticService, RaceStateManager raceStateManager)
    {
        _configuration = configuration;
        _logger = logger;
        _dataLoggingService = dataLoggingService;
        _diagnosticService = diagnosticService;
        _raceStateManager = raceStateManager;
    }

    public Task StartAsync()
    {
        var timingPort = _configuration.GetValue<int>("TcpSettings:TimingPort");
        var resultsPort = _configuration.GetValue<int>("TcpSettings:ResultsPort");
        var bufferSize = _configuration.GetValue<int>("TcpSettings:BufferSize");

        _logger.LogInformation("Starting TCP listeners on timing port {TimingPort} and results port {ResultsPort}", timingPort, resultsPort);

        // Start timing listener
        _timingListener = new TcpListener(IPAddress.Any, timingPort);
        _timingListener.Start();

        // Start results listener
        _resultsListener = new TcpListener(IPAddress.Any, resultsPort);
        _resultsListener.Start();

        _cancellationTokenSource = new CancellationTokenSource();

        _logger.LogInformation("TCP listeners started successfully");

        // Start accepting connections in background tasks
        _ = Task.Run(() => AcceptConnectionsAsync(_timingListener, "TIMING", _cancellationTokenSource.Token));
        _ = Task.Run(() => AcceptConnectionsAsync(_resultsListener, "RESULTS", _cancellationTokenSource.Token));
        
        return Task.CompletedTask;
    }

    private async Task AcceptConnectionsAsync(TcpListener listener, string connectionType, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await listener.AcceptTcpClientAsync();
                _logger.LogInformation("{ConnectionType} client connected from {RemoteEndPoint}", connectionType, tcpClient.Client.RemoteEndPoint);

                // Handle each client connection in a separate task
                _ = Task.Run(() => HandleClientAsync(tcpClient, connectionType, cancellationToken));
            }
            catch (ObjectDisposedException)
            {
                // TcpListener was disposed, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting {ConnectionType} TCP connection", connectionType);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, string connectionType, CancellationToken cancellationToken)
    {
        var bufferSize = _configuration.GetValue<int>("TcpSettings:BufferSize");
        var buffer = new byte[bufferSize];

        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                _logger.LogInformation("Handling {ConnectionType} client connection from {RemoteEndPoint}", connectionType, client.Client.RemoteEndPoint);

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        _logger.LogInformation("{ConnectionType} client disconnected: {RemoteEndPoint}", connectionType, client.Client.RemoteEndPoint);
                        break;
                    }

                    // Log the received data
                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    
                    // Run diagnostic analysis with connection type
                    _diagnosticService.AnalyzeData(data, $"{client.Client.RemoteEndPoint} ({connectionType})");
                    
                    // Process the message through the race state manager
                    await _raceStateManager.ProcessMessageAsync(data, $"{client.Client.RemoteEndPoint} ({connectionType})");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {ConnectionType} client connection from {RemoteEndPoint}", connectionType, client.Client.RemoteEndPoint);
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping TCP listeners");
        _cancellationTokenSource?.Cancel();
        _timingListener?.Stop();
        _resultsListener?.Stop();
        _cancellationTokenSource?.Dispose();
    }
}
