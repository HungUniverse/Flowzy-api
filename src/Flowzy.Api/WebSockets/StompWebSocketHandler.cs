using System.Net.WebSockets;
using System.Text;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Authentication;

namespace Flowzy.Api.WebSockets;

public sealed class StompWebSocketHandler(
    IJwtService jwt,
    ITokenBlacklistService blacklist,
    IAccountRepository accounts,
    WebSocketOptions options,
    ILogger<StompWebSocketHandler> logger)
{
    public async Task HandleAsync(HttpContext context)
    {
        // Check at the acceptance boundary too, including servers that supply their own WebSocket feature.
        if (context.Request.Headers.TryGetValue("Origin", out var origin) &&
            (origin.Count != 1 || !options.AllowedOrigins.Contains(origin[0]!, StringComparer.OrdinalIgnoreCase)))
        { context.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
        if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[16 * 1024]; string? authenticatedEmail = null;
        while (socket.State == WebSocketState.Open)
        {
            var message = new StringBuilder(); WebSocketReceiveResult received;
            do
            {
                received = await socket.ReceiveAsync(buffer, context.RequestAborted);
                if (received.MessageType == WebSocketMessageType.Close) { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, context.RequestAborted); return; }
                message.Append(Encoding.UTF8.GetString(buffer, 0, received.Count));
            } while (!received.EndOfMessage);

            foreach (var frame in message.ToString().Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                var (command, headers) = Parse(frame);
                if (command is "CONNECT" or "STOMP")
                {
                    try
                    {
                        var authorization = headers.GetValueOrDefault("Authorization") ?? headers.GetValueOrDefault("authorization");
                        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal)) throw new InvalidOperationException("Missing or invalid Authorization header");
                        var token = authorization[7..]; if (await blacklist.IsBlacklistedAsync(token, context.RequestAborted)) throw new InvalidOperationException("Token is blacklisted");
                        var principal = jwt.ValidateToken(token); authenticatedEmail = principal.Identity?.Name ?? principal.FindFirst("sub")?.Value;
                        var account = authenticatedEmail is null ? null : await accounts.FindByEmailAsync(authenticatedEmail, context.RequestAborted);
                        if (account is null || account.Status != "ACTIVE") throw new InvalidOperationException("Invalid or disabled user token");
                        await Send(socket, "CONNECTED\nversion:1.2\nheart-beat:0,0\n\n\0", context.RequestAborted);
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "STOMP authentication failed");
                        await Send(socket, $"ERROR\nmessage:{Escape(exception.Message)}\n\n\0", context.RequestAborted);
                        await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication failed", context.RequestAborted); return;
                    }
                }
                else if (command == "SUBSCRIBE")
                {
                    if (authenticatedEmail is null || headers.GetValueOrDefault("destination") != "/user/queue/notifications")
                    { await Send(socket, "ERROR\nmessage:Subscription destination is not allowed\n\n\0", context.RequestAborted); }
                    else if (headers.TryGetValue("receipt", out var receipt))
                    { await Send(socket, $"RECEIPT\nreceipt-id:{Escape(receipt)}\n\n\0", context.RequestAborted); }
                }
                else if (command == "SEND") await Send(socket, "ERROR\nmessage:Client SEND is not supported\n\n\0", context.RequestAborted);
                else if (command == "DISCONNECT") { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, context.RequestAborted); return; }
            }
        }
    }

    private static (string Command, Dictionary<string, string> Headers) Parse(string frame)
    {
        var lines = frame.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines.Skip(1).TakeWhile(x => x.Length > 0))
        { var colon = line.IndexOf(':'); if (colon > 0) headers[line[..colon]] = line[(colon + 1)..]; }
        return (lines[0].Trim().ToUpperInvariant(), headers);
    }

    private static Task Send(WebSocket socket, string value, CancellationToken ct) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(value), WebSocketMessageType.Text, true, ct);
    private static string Escape(string value) => value.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
