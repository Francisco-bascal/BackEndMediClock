using System.ComponentModel.DataAnnotations;
using BackEndMediClock.Models;

namespace BackEndMediClock.DTOs
{
    public class EventoDto
    {
        public int EventoId { get; set; }

        public DateTime FechaHora { get; set; }

        public TipoEvento Tipo { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public int DispositivoId { get; set; }

        public int? AlarmaId { get; set; }
    }

    public class CreateEventoDto
    {
        [Required]
        public TipoEvento Tipo { get; set; }

        [Required, StringLength(300, MinimumLength = 10, ErrorMessage = "La longitud de la descripción debe estar entre 10 y 300 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        public int? AlarmaId { get; set; }
    }
}