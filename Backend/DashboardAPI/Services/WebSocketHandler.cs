using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DashboardAPI.Services;

public class WebSocketHandler
{
    private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _sockets = new();
    private readonly ILogger<WebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task HandleWebSocketConnection(System.Net.WebSockets.WebSocket webSocket)
    {
        var socketId = Guid.NewGuid().ToString();
        _sockets.TryAdd(socketId, webSocket);
        _logger.LogInformation("New WebSocket connection established. ID: {SocketId}", socketId);

        try
        {
            await HandleWebSocketCommunication(webSocket, socketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket communication for socket {SocketId}", socketId);
        }
        finally
        {
            await RemoveSocket(socketId);
        }
    }

    private async Task HandleWebSocketCommunication(System.Net.WebSockets.WebSocket webSocket, string socketId)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested close", CancellationToken.None);
                    break;
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket connection closed unexpectedly for socket {SocketId}", socketId);
                break;
            }
        }
    }

    private async Task RemoveSocket(string socketId)
    {
        if (_sockets.TryRemove(socketId, out var socket))
        {
            _logger.LogInformation("WebSocket connection removed. ID: {SocketId}", socketId);
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
            }
            socket.Dispose();
        }
    }

    public async Task BroadcastMetrics(object metrics, CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(metrics, _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(message);
        var deadSockets = new List<string>();

        foreach (var (socketId, socket) in _sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                deadSockets.Add(socketId);
                continue;
            }

            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting metrics to socket {SocketId}", socketId);
                deadSockets.Add(socketId);
            }
        }

        foreach (var socketId in deadSockets)
        {
            await RemoveSocket(socketId);
        }
    }
} 