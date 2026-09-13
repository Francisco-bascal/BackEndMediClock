using BackEndMediClock.Common;
using BackEndMediClock.DTOs;
using BackEndMediClock.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackEndMediClock.Controllers
{
    [ApiController]
    [Route("api/dispositivos")]
    public class DispositivosController : ControllerBase
    {
        private readonly DispositivoService _dispositivoService;

        public DispositivosController(DispositivoService dispositivoService)
        {
            _dispositivoService = dispositivoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DispositivoDto>>> ObtenerTodos(
            CancellationToken cancellationToken = default)
        {
            var dispositivos = await _dispositivoService.ObtenerTodosAsync(cancellationToken);
            return Ok(dispositivos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DispositivoDto>> Obtener(
            int id,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _dispositivoService.ObtenerPorIdAsync(id, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpGet("{id:int}/configuracion")]
        public async Task<ActionResult<ConfiguracionDispositivoDto>> ObtenerConfiguracion(
            int id,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _dispositivoService.ObtenerConfiguracionAsync(id, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpPost]
        public async Task<ActionResult<DispositivoDto>> Crear(
            CreateDispositivoDto dto,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _dispositivoService.CrearAsync(dto, cancellationToken);

            if (!resultado.IsValid)
            {
                return MapeadorResultado.DeResultado(resultado);
            }

            return CreatedAtAction(nameof(Obtener), new { id = resultado.Value!.DispositivoId }, resultado.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<DispositivoDto>> Actualizar(
            int id,
            UpdateDispositivoDto dto,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _dispositivoService.ActualizarAsync(id, dto, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(
            int id,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _dispositivoService.EliminarAsync(id, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }
    }
}