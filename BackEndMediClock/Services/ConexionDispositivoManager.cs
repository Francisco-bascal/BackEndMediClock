using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BackEndMediClock.Services
{
    public class ConexionDispositivoManager
    {
        private readonly ConcurrentDictionary<int, WebSocket> _sockets = new();
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        public void Registrar(int dispositivoId, WebSocket socket)
        {
            if (_sockets.TryGetValue(dispositivoId, out var anterior) && anterior != socket)
            {
                try
                {
                    anterior.CloseAsync(WebSocketCloseStatus.NormalClosure, "reemplazado", CancellationToken.None).Wait();
                }
                catch
                {
                    // Ignorar errores al cerrar el socket anterior.
                }
            }

            _sockets[dispositivoId] = socket;
            _locks.TryAdd(dispositivoId, new SemaphoreSlim(1, 1));
        }

        public void Desregistrar(int dispositivoId, WebSocket socket)
        {
            if (_sockets.TryGetValue(dispositivoId, out var actual) && actual == socket)
            {
                _sockets.TryRemove(dispositivoId, out _);
            }
        }

        public bool EstaConectado(int dispositivoId)
            => _sockets.TryGetValue(dispositivoId, out var socket) && socket.State == WebSocketState.Open;

        public async Task<bool> EnviarAsync(int dispositivoId, string mensaje, CancellationToken cancellationToken = default)
        {
            if (!_sockets.TryGetValue(dispositivoId, out var socket) || socket.State != WebSocketState.Open)
            {
                return false;
            }

            var semaforo = _locks.GetOrAdd(dispositivoId, _ => new SemaphoreSlim(1, 1));
            await semaforo.WaitAsync(cancellationToken);

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(mensaje);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                semaforo.Release();
            }
        }
    }
}