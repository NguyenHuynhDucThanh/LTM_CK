using server.Managers;
using server.Services;
using server.WebSocketGateway;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8888");

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<QuestionManager>();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<ScoreService>();
builder.Services.AddSingleton<GameManager>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddHostedService<HeartbeatService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleConnectionAsync(socket);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
