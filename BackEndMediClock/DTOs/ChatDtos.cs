using System.ComponentModel.DataAnnotations;

namespace BackEndMediClock.DTOs
{
    public class MensajeChatDto
    {
        [Required]
        public string Rol { get; set; } = string.Empty; // "user" o "model" / "usuario" o "asistente"

        [Required]
        public string Contenido { get; set; } = string.Empty;
    }

    public class SolicitudChatDto
    {
        [Required]
        public int DispositivoId { get; set; }

        [Required]
        public string Mensaje { get; set; } = string.Empty;

        public List<MensajeChatDto> Historial { get; set; } = new();
    }

    public class RespuestaChatDto
    {
        public string Respuesta { get; set; } = string.Empty;
        public bool AccionEjecutada { get; set; }
        public string? DetalleAccion { get; set; }
    }
}
