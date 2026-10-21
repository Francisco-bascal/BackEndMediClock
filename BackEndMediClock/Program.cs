using System.Net.WebSockets;
using BackEndMediClock.Data;
using BackEndMediClock.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// CORS para el front (local en desarrollo).
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5000",
                "http://localhost:5052",
                "http://localhost:5173",
                "https://localhost:5001",
                "https://localhost:7000",
                "https://localhost:7273")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MediClockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DispositivoService>();
builder.Services.AddScoped<AlarmaService>();
builder.Services.AddScoped<EventoService>();
builder.Services.AddSingleton<ConexionDispositivoManager>();
builder.Services.AddHttpClient<GeminiChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseWebSockets();

app.UseHttpsRedirection();

app.UseAuthorization();

app.Map("/ws/dispositivo/{id:int}", async (
    HttpContext context,
    int id,
    MediClockDbContext db,
    DispositivoService dispositivoService,
    CancellationToken cancellationToken) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var existe = await db.Dispositivos.AnyAsync(d => d.DispositivoId == id, cancellationToken);
    if (!existe)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var manager = context.RequestServices.GetRequiredService<ConexionDispositivoManager>();
    manager.Registrar(id, socket);

    await dispositivoService.EntregarComandosPendientesAsync(id, socket, cancellationToken);

    var buffer = new byte[1024];
    try
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var resultado = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (resultado.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }
    }
    catch
    {
        // Conexión cerrada o error de red: se desregistra abajo.
    }
    finally
    {
        manager.Desregistrar(id, socket);
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch
            {
                // Ignorar errores al cerrar.
            }
        }
    }
});

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MediClockDbContext>();
    db.Database.Migrate();
}

app.MapFallbackToFile("index.html");

app.Run();