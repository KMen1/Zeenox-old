using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discordance.Models.Socket.Client;
using Discordance.Services;
using Microsoft.AspNetCore.Mvc;

namespace Discordance.Controllers;

public class WebSocketController : ControllerBase
{
    private readonly SocketHelper _socketHelper;

    public WebSocketController(AudioService audioService)
    {
        _socketHelper = audioService.SocketHelper;
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
            await _socketHelper.HandleClientMessage(message, socket, cancellationToken).ConfigureAwait(false);
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