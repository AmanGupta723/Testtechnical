using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace DashboardAPI.WebSocket
{
    public class WebSocketHandler
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _sockets = new();

        public async Task OnConnected(System.Net.WebSockets.WebSocket socket)
        {
            string socketId = Guid.NewGuid().ToString();
            _sockets.TryAdd(socketId, socket);

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    await Receive(socket, async (result, buffer) =>
                    {
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await HandleDisconnect(socketId);
                            return;
                        }
                    });
                }
                catch
                {
                    await HandleDisconnect(socketId);
                }
            }
        }

        public async Task BroadcastAsync(string message, CancellationToken ct = default)
        {
            foreach (var socket in _sockets)
            {
                if (socket.Value.State == WebSocketState.Open)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                    var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                    await socket.Value.SendAsync(arraySegment, WebSocketMessageType.Text, true, ct);
                }
            }
        }

        private async Task HandleDisconnect(string socketId)
        {
            if (_sockets.TryRemove(socketId, out var socket))
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the handler", CancellationToken.None);
            }
        }

        private async Task Receive(System.Net.WebSockets.WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                handleMessage(result, buffer);
            }
        }
    }
}