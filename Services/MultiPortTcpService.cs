using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace WebLynx.Services;

public class MultiPortTcpService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MultiPortTcpService> _logger;
    private readonly DataLoggingService _dataLoggingService;
    private readonly DiagnosticService _diagnosticService;
    private readonly RaceStateManager _raceStateManager;
    private TcpListener? _timingListener;
    private TcpListener? _resultsListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly List<TcpClient> _activeConnections = new();
    private readonly object _connectionsLock = new();
    private bool _isStopped = false;

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

        try
        {
            // Start timing listener
            _timingListener = new TcpListener(IPAddress.Any, timingPort);
            _timingListener.Start();
            _logger.LogInformation("Timing listener started on {EndPoint}", _timingListener.LocalEndpoint);

            // Start results listener
            _resultsListener = new TcpListener(IPAddress.Any, resultsPort);
            _resultsListener.Start();
            _logger.LogInformation("Results listener started on {EndPoint}", _resultsListener.LocalEndpoint);

            _cancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("TCP listeners started successfully");

            // Start accepting connections in background tasks
            _ = Task.Run(() => AcceptConnectionsAsync(_timingListener, "TIMING", _cancellationTokenSource.Token));
            _ = Task.Run(() => AcceptConnectionsAsync(_resultsListener, "RESULTS", _cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP listeners");
            throw;
        }
        
        return Task.CompletedTask;
    }

    private async Task AcceptConnectionsAsync(TcpListener listener, string connectionType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to accept {ConnectionType} connections on port {Port}", connectionType, ((IPEndPoint)listener.LocalEndpoint).Port);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Use AcceptTcpClientAsync with cancellation token
                var tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
                _logger.LogInformation("{ConnectionType} client connected from {RemoteEndPoint}", connectionType, tcpClient.Client.RemoteEndPoint);

                // Track the active connection
                lock (_connectionsLock)
                {
                    _activeConnections.Add(tcpClient);
                }

                // Handle each client connection in a separate task
                _ = Task.Run(() => HandleClientAsync(tcpClient, connectionType, cancellationToken));
            }
            catch (ObjectDisposedException)
            {
                // TcpListener was disposed, exit gracefully
                _logger.LogInformation("{ConnectionType} listener was disposed, stopping acceptance", connectionType);
                break;
            }
            catch (OperationCanceledException)
            {
                // Cancellation was requested
                _logger.LogInformation("{ConnectionType} listener cancellation requested, stopping acceptance", connectionType);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting {ConnectionType} TCP connection", connectionType);
                // Add a small delay to prevent rapid error loops
                await Task.Delay(1000, cancellationToken);
            }
        }
        
        _logger.LogInformation("Stopped accepting {ConnectionType} connections", connectionType);
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
        finally
        {
            // Remove the connection from tracking when done
            lock (_connectionsLock)
            {
                _activeConnections.Remove(client);
            }
        }
    }

    public void Stop()
    {
        if (_isStopped)
        {
            return;
        }
        
        _isStopped = true;
        _logger.LogInformation("Stopping TCP listeners");
        
        // Cancel all operations immediately
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, ignore
        }
        
        // Close all active client connections
        List<TcpClient> connectionsToClose;
        lock (_connectionsLock)
        {
            connectionsToClose = new List<TcpClient>(_activeConnections);
        }
        
        foreach (var connection in connectionsToClose)
        {
            try
            {
                if (connection.Connected)
                {
                    // Explicitly shutdown the socket first to ensure clean termination
                    connection.Client.Shutdown(SocketShutdown.Both);
                    connection.Close();
                    _logger.LogInformation("Closed client connection from {RemoteEndPoint}", connection.Client.RemoteEndPoint);
                }
                connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing client connection from {RemoteEndPoint}", connection.Client.RemoteEndPoint);
            }
        }
        
        // Stop listeners immediately
        try
        {
            _timingListener?.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping timing listener");
        }
        
        try
        {
            _resultsListener?.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping results listener");
        }
        
        // Dispose resources
        _cancellationTokenSource?.Dispose();
        _timingListener = null;
        _resultsListener = null;
        
        // Clear the connections list
        lock (_connectionsLock)
        {
            _activeConnections.Clear();
        }
        
        _logger.LogInformation("TCP listeners stopped and all connections closed");
    }

    public void Dispose()
    {
        Stop();
    }
}
