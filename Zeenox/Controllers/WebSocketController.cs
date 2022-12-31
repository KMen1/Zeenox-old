using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zeenox.Models.Socket.Client;
using Zeenox.Services;

namespace Zeenox.Controllers;

public class WebSocketController : ControllerBase
{
    private readonly SocketService _socketService;

    public WebSocketController(SocketService socketService)
    {
        _socketService = socketService;
    }

    [Route("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await ReceiveMessageAsync(webSocket).ConfigureAwait(false);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }

    private async Task ReceiveMessageAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await socket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        while (!receiveResult.CloseStatus.HasValue)
        {
            var rawData = Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, receiveResult.Count));
            var message = JsonSerializer.Deserialize<BaseClientMessage>(rawData);
            await _socketService.HandleClientMessage(message, socket, cancellationToken).ConfigureAwait(false);
            receiveResult = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
        }

        cancellationTokenSource.Cancel();
        await socket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None).ConfigureAwait(false);
    }
}