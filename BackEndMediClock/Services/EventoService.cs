using System.Linq.Expressions;
using BackEndMediClock.Common;
using BackEndMediClock.Data;
using BackEndMediClock.DTOs;
using BackEndMediClock.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEndMediClock.Services
{
    public class EventoService
    {
        private const int TamanioMaximoPaginacion = 100;

        private static readonly Expression<Func<Evento, EventoDto>> ProyeccionEvento = e => new EventoDto
        {
            EventoId = e.EventoId,
            FechaHora = e.FechaHora,
            Tipo = e.Tipo,
            Descripcion = e.Descripcion,
            DispositivoId = e.DispositivoId,
            AlarmaId = e.AlarmaId
        };

        private readonly MediClockDbContext _context;

        public EventoService(MediClockDbContext context)
        {
            _context = context;
        }

        public async Task<Resultado<List<EventoDto>>> ObtenerEventosPorDispositivoAsync(
            int dispositivoId,
            int pagina = 1,
            int tamanioPagina = 50,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<List<EventoDto>>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var paginaSegura = Math.Max(1, pagina);
            var tamanioSeguro = Math.Clamp(tamanioPagina, 1, TamanioMaximoPaginacion);

            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.DispositivoId == dispositivoId)
                .OrderByDescending(e => e.FechaHora)
                .Skip((paginaSegura - 1) * tamanioSeguro)
                .Take(tamanioSeguro)
                .Select(ProyeccionEvento)
                .ToListAsync(cancellationToken);

            return Resultado<List<EventoDto>>.Success(eventos);
        }

        public async Task<Resultado<EventoDto>> ObtenerPorIdAsync(
            int dispositivoId,
            int eventoId,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<EventoDto>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var evento = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.EventoId == eventoId && e.DispositivoId == dispositivoId)
                .Select(ProyeccionEvento)
                .FirstOrDefaultAsync(cancellationToken);

            return evento is null
                ? Resultado<EventoDto>.NoEncontrado($"No existe un evento con el id {eventoId} para el dispositivo {dispositivoId}.")
                : Resultado<EventoDto>.Success(evento);
        }

        public async Task<Resultado<EventoDto>> RegistrarAsync(
            int dispositivoId,
            CreateEventoDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<EventoDto>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            if (dto.AlarmaId.HasValue && !await ExisteAlarmaAsync(dispositivoId, dto.AlarmaId.Value, cancellationToken))
            {
                return Resultado<EventoDto>.NoEncontrado(
                    $"No existe una alarma con el id {dto.AlarmaId.Value} para el dispositivo {dispositivoId}.");
            }

            var evento = new Evento
            {
                DispositivoId = dispositivoId,
                FechaHora = DateTime.Now,
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion,
                AlarmaId = dto.AlarmaId
            };

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado<EventoDto>.Success(CrearEventoDto(evento));
        }

        private async Task<bool> ExisteDispositivoAsync(int dispositivoId, CancellationToken cancellationToken)
            => await _context.Dispositivos.AnyAsync(d => d.DispositivoId == dispositivoId, cancellationToken);

        private async Task<bool> ExisteAlarmaAsync(int dispositivoId, int alarmaId, CancellationToken cancellationToken)
            => await _context.Alarmas.AnyAsync(
                a => a.AlarmaId == alarmaId && a.DispositivoId == dispositivoId,
                cancellationToken);

        private static EventoDto CrearEventoDto(Evento evento) => new()
        {
            EventoId = evento.EventoId,
            FechaHora = evento.FechaHora,
            Tipo = evento.Tipo,
            Descripcion = evento.Descripcion,
            DispositivoId = evento.DispositivoId,
            AlarmaId = evento.AlarmaId
        };
    }
}