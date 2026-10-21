using BackEndMediClock.Common;
using BackEndMediClock.DTOs;
using BackEndMediClock.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackEndMediClock.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly GeminiChatService _chatService;

        public ChatController(GeminiChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<RespuestaChatDto>> EnviarMensaje(
            [FromBody] SolicitudChatDto solicitud,
            CancellationToken cancellationToken = default)
        {
            var resultado = await _chatService.ProcesarMensajeAsync(solicitud, cancellationToken);
            return MapeadorResultado.DeResultado(resultado);
        }
    }
}
