using BackEndMediClock.Common;
using BackEndMediClock.Data;
using BackEndMediClock.DTOs;
using BackEndMediClock.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEndMediClock.Services
{
    public class DispositivoService
    {
        private readonly MediClockDbContext _context;

        public DispositivoService(MediClockDbContext context)
        {
            _context = context;
        }

        public async Task<List<DispositivoDto>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Dispositivos
                .AsNoTracking()
                .OrderBy(d => d.Nombre)
                .Select(d => new DispositivoDto
                {
                    DispositivoId = d.DispositivoId,
                    Nombre = d.Nombre
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<Resultado<DispositivoDto>> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var dispositivo = await ObtenerDispositivoDtoAsync(id, cancellationToken);

            return dispositivo is null
                ? Resultado<DispositivoDto>.NoEncontrado($"No existe un dispositivo con el id {id}.")
                : Resultado<DispositivoDto>.Success(dispositivo);
        }

        public async Task<Resultado<ConfiguracionDispositivoDto>> ObtenerConfiguracionAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var configuracion = await _context.Dispositivos
                .AsNoTracking()
                .Where(d => d.DispositivoId == id)
                .Select(d => new ConfiguracionDispositivoDto
                {
                    DispositivoId = d.DispositivoId,
                    Nombre = d.Nombre,
                    Alarmas = d.Alarmas
                        .OrderBy(a => a.DiaSemana)
                        .ThenBy(a => a.NumeroAlarma)
                        .Select(a => new AlarmaDto
                        {
                            AlarmaId = a.AlarmaId,
                            DiaSemana = a.DiaSemana,
                            NumeroAlarma = a.NumeroAlarma,
                            Hora = a.Hora,
                            DispositivoId = a.DispositivoId
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return configuracion is null
                ? Resultado<ConfiguracionDispositivoDto>.NoEncontrado($"No existe un dispositivo con el id {id}.")
                : Resultado<ConfiguracionDispositivoDto>.Success(configuracion);
        }

        public async Task<Resultado<DispositivoDto>> CrearAsync(
            CreateDispositivoDto dto,
            CancellationToken cancellationToken = default)
        {
            var dispositivo = new Dispositivo
            {
                Nombre = dto.Nombre
            };

            _context.Dispositivos.Add(dispositivo);
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado<DispositivoDto>.Success(CrearDispositivoDto(dispositivo));
        }

        public async Task<Resultado<DispositivoDto>> ActualizarAsync(
            int id,
            UpdateDispositivoDto dto,
            CancellationToken cancellationToken = default)
        {
            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.DispositivoId == id, cancellationToken);

            if (dispositivo is null)
            {
                return Resultado<DispositivoDto>.NoEncontrado($"No existe un dispositivo con el id {id}.");
            }

            dispositivo.Nombre = dto.Nombre;
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado<DispositivoDto>.Success(CrearDispositivoDto(dispositivo));
        }

        public async Task<Resultado> EliminarAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.DispositivoId == id, cancellationToken);

            if (dispositivo is null)
            {
                return Resultado.NoEncontrado($"No existe un dispositivo con el id {id}.");
            }

            var tieneDependencias = await _context.Alarmas.AnyAsync(a => a.DispositivoId == id, cancellationToken)
                || await _context.Eventos.AnyAsync(e => e.DispositivoId == id, cancellationToken);

            if (tieneDependencias)
            {
                return Resultado.Conflicto("El dispositivo tiene alarmas o eventos asociados y no puede eliminarse.");
            }

            _context.Dispositivos.Remove(dispositivo);
            await _context.SaveChangesAsync(cancellationToken);

            return Resultado.Success();
        }

        private async Task<DispositivoDto?> ObtenerDispositivoDtoAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Dispositivos
                .AsNoTracking()
                .Where(d => d.DispositivoId == id)
                .Select(d => new DispositivoDto
                {
                    DispositivoId = d.DispositivoId,
                    Nombre = d.Nombre
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static DispositivoDto CrearDispositivoDto(Dispositivo dispositivo) => new()
        {
            DispositivoId = dispositivo.DispositivoId,
            Nombre = dispositivo.Nombre
        };
    }
}