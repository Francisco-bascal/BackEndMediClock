using System.Linq.Expressions;
using BackEndMediClock.Common;
using BackEndMediClock.Data;
using BackEndMediClock.DTOs;
using BackEndMediClock.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEndMediClock.Services
{
    public class AlarmaService
    {
        private static readonly Expression<Func<Alarma, AlarmaDto>> ProyeccionAlarma = a => new AlarmaDto
        {
            AlarmaId = a.AlarmaId,
            DiaSemana = a.DiaSemana,
            NumeroAlarma = a.NumeroAlarma,
            Hora = a.Hora,
            DispositivoId = a.DispositivoId
        };

        private readonly MediClockDbContext _context;

        public AlarmaService(MediClockDbContext context)
        {
            _context = context;
        }

        public async Task<Resultado<List<AlarmaDto>>> ObtenerAlarmasPorDispositivoAsync(
            int dispositivoId,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<List<AlarmaDto>>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var alarmas = await _context.Alarmas
                .AsNoTracking()
                .Where(a => a.DispositivoId == dispositivoId)
                .OrderBy(a => a.DiaSemana)
                .ThenBy(a => a.NumeroAlarma)
                .Select(ProyeccionAlarma)
                .ToListAsync(cancellationToken);

            return Resultado<List<AlarmaDto>>.Success(alarmas);
        }

        public async Task<Resultado<AlarmaDto>> ObtenerPorIdAsync(
            int dispositivoId,
            int alarmaId,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<AlarmaDto>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var alarma = await _context.Alarmas
                .AsNoTracking()
                .Where(a => a.AlarmaId == alarmaId && a.DispositivoId == dispositivoId)
                .Select(ProyeccionAlarma)
                .FirstOrDefaultAsync(cancellationToken);

            return alarma is null
                ? Resultado<AlarmaDto>.NoEncontrado($"No existe una alarma con el id {alarmaId} para el dispositivo {dispositivoId}.")
                : Resultado<AlarmaDto>.Success(alarma);
        }

        public async Task<Resultado<AlarmaDto>> CrearAsync(
            int dispositivoId,
            CreateAlarmaDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<AlarmaDto>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            if (await ExisteAlarmaAsync(dispositivoId, dto.DiaSemana, dto.NumeroAlarma, null, cancellationToken))
            {
                return Resultado<AlarmaDto>.Conflicto(
                    $"Ya existe la alarma {dto.NumeroAlarma} para el día {dto.DiaSemana} del dispositivo {dispositivoId}.");
            }

            var alarma = new Alarma
            {
                DispositivoId = dispositivoId,
                DiaSemana = dto.DiaSemana,
                NumeroAlarma = dto.NumeroAlarma,
                Hora = dto.Hora
            };

            _context.Alarmas.Add(alarma);
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado<AlarmaDto>.Success(CrearAlarmaDto(alarma));
        }

        public async Task<Resultado<AlarmaDto>> ActualizarAsync(
            int dispositivoId,
            int alarmaId,
            UpdateAlarmaDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado<AlarmaDto>.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var alarma = await _context.Alarmas
                .Where(a => a.AlarmaId == alarmaId && a.DispositivoId == dispositivoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (alarma is null)
            {
                return Resultado<AlarmaDto>.NoEncontrado($"No existe una alarma con el id {alarmaId} para el dispositivo {dispositivoId}.");
            }

            if (await ExisteAlarmaAsync(dispositivoId, dto.DiaSemana, dto.NumeroAlarma, alarmaId, cancellationToken))
            {
                return Resultado<AlarmaDto>.Conflicto(
                    $"Ya existe la alarma {dto.NumeroAlarma} para el día {dto.DiaSemana} del dispositivo {dispositivoId}.");
            }

            alarma.DiaSemana = dto.DiaSemana;
            alarma.NumeroAlarma = dto.NumeroAlarma;
            alarma.Hora = dto.Hora;
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado<AlarmaDto>.Success(CrearAlarmaDto(alarma));
        }

        public async Task<Resultado> EliminarAsync(
            int dispositivoId,
            int alarmaId,
            CancellationToken cancellationToken = default)
        {
            if (!await ExisteDispositivoAsync(dispositivoId, cancellationToken))
            {
                return Resultado.NoEncontrado($"No existe un dispositivo con el id {dispositivoId}.");
            }

            var alarma = await _context.Alarmas
                .Where(a => a.AlarmaId == alarmaId && a.DispositivoId == dispositivoId)
                .FirstOrDefaultAsync(cancellationToken);

            if (alarma is null)
            {
                return Resultado.NoEncontrado($"No existe una alarma con el id {alarmaId} para el dispositivo {dispositivoId}.");
            }

            var tieneEventos = await _context.Eventos.AnyAsync(e => e.AlarmaId == alarmaId, cancellationToken);

            if (tieneEventos)
            {
                return Resultado.Conflicto($"La alarma {alarmaId} tiene eventos asociados y no puede eliminarse.");
            }

            _context.Alarmas.Remove(alarma);
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado.Success();
        }

        private async Task<bool> ExisteDispositivoAsync(int dispositivoId, CancellationToken cancellationToken)
            => await _context.Dispositivos.AnyAsync(d => d.DispositivoId == dispositivoId, cancellationToken);

        private async Task<bool> ExisteAlarmaAsync(
            int dispositivoId,
            int diaSemana,
            int numeroAlarma,
            int? alarmaIdExcluida,
            CancellationToken cancellationToken)
            => await _context.Alarmas.AnyAsync(
                a => a.DispositivoId == dispositivoId
                    && a.DiaSemana == diaSemana
                    && a.NumeroAlarma == numeroAlarma
                    && (!alarmaIdExcluida.HasValue || a.AlarmaId != alarmaIdExcluida.Value),
                cancellationToken);

        private static AlarmaDto CrearAlarmaDto(Alarma alarma) => new()
        {
            AlarmaId = alarma.AlarmaId,
            DiaSemana = alarma.DiaSemana,
            NumeroAlarma = alarma.NumeroAlarma,
            Hora = alarma.Hora,
            DispositivoId = alarma.DispositivoId
        };
    }
}