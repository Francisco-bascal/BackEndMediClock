using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEndMediClock.Models
{
    public class Evento
    {
        [Key]
        public int EventoId { get; set; }
        [Required]
        public DateTime FechaHora { get; set; }
        [Required]
        public TipoEvento Tipo { get; set; }
        [StringLength(300, MinimumLength = 10, ErrorMessage = "La longitud de la descripción debe estar entre 10 y 300 caracteres")]
        public string Descripcion { get; set; } = string.Empty;


        [ForeignKey(nameof(Dispositivo))]
        public int DispositivoId { get; set; }
        public Dispositivo Dispositivo { get; set; } = null!;

        [ForeignKey(nameof(Alarma))]
        public int? AlarmaId { get; set; }
        public Alarma? Alarma { get; set; } = null!;
    }

    public enum TipoEvento 
    {
        AlarmaActivada,
        DosisEntregada,
        LogError
    }
}