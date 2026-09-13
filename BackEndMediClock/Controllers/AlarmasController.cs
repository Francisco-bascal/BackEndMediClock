using BackEndMediClock.Common;
using BackEndMediClock.DTOs;
using BackEndMediClock.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackEndMediClock.Controllers
{
    [ApiController]
    [Route("api/dispositivos/{dispositivoId:int}/alarmas")]
    public class AlarmasController : ControllerBase
    {
        private readonly AlarmaService _alarmaService;

        public AlarmasController(AlarmaService alarmaService)
        {
            _alarmaService = alarmaService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AlarmaDto>>> ObtenerAlarmas(
            int dispositivoId,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _alarmaService.ObtenerAlarmasPorDispositivoAsync(dispositivoId, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpGet("{alarmaId:int}")]
        public async Task<ActionResult<AlarmaDto>> Obtener(
            int dispositivoId,
            int alarmaId,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _alarmaService.ObtenerPorIdAsync(dispositivoId, alarmaId, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpPost]
        public async Task<ActionResult<AlarmaDto>> Crear(
            int dispositivoId,
            CreateAlarmaDto dto,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _alarmaService.CrearAsync(dispositivoId, dto, cancellationToken);

            if (!resultado.IsValid)
            {
                return MapeadorResultado.DeResultado(resultado);
            }

            return CreatedAtAction(
                nameof(Obtener),
                new { dispositivoId, alarmaId = resultado.Value!.AlarmaId },
                resultado.Value);
        }

        [HttpPut("{alarmaId:int}")]
        public async Task<ActionResult<AlarmaDto>> Actualizar(
            int dispositivoId,
            int alarmaId,
            UpdateAlarmaDto dto,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _alarmaService.ActualizarAsync(dispositivoId, alarmaId, dto, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpDelete("{alarmaId:int}")]
        public async Task<IActionResult> Eliminar(
            int dispositivoId,
            int alarmaId,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _alarmaService.EliminarAsync(dispositivoId, alarmaId, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }
    }
}