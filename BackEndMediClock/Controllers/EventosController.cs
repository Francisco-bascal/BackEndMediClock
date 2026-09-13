using BackEndMediClock.Common;
using BackEndMediClock.DTOs;
using BackEndMediClock.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackEndMediClock.Controllers
{
    [ApiController]
    [Route("api/dispositivos/{dispositivoId:int}/eventos")]
    public class EventosController : ControllerBase
    {
        private readonly EventoService _eventoService;

        public EventosController(EventoService eventoService)
        {
            _eventoService = eventoService;
        }

        [HttpGet]
        public async Task<ActionResult<List<EventoDto>>> ObtenerEventos(
            int dispositivoId,
            int pagina = 1,
            int tamanioPagina = 50,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _eventoService.ObtenerEventosPorDispositivoAsync(
                dispositivoId,
                pagina,
                tamanioPagina,
                cancellationToken);

            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpGet("{eventoId:int}")]
        public async Task<ActionResult<EventoDto>> Obtener(
            int dispositivoId,
            int eventoId,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _eventoService.ObtenerPorIdAsync(dispositivoId, eventoId, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }

        [HttpPost]
        public async Task<ActionResult<EventoDto>> Registrar(
            int dispositivoId,
            CreateEventoDto dto,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _eventoService.RegistrarAsync(dispositivoId, dto, cancellationToken);

            if (!resultado.IsValid)
            {
                return MapeadorResultado.DeResultado(resultado);
            }

            return CreatedAtAction(
                nameof(Obtener),
                new { dispositivoId, eventoId = resultado.Value!.EventoId },
                resultado.Value);
        }
    }
}