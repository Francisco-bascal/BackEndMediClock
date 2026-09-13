using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEndMediClock.Models
{
    //Record para que el registro sea inmutable, se crea y queda tal cual es
    public record Evento
    {
        [Key]
        public int EventoId { get; set; }
        [Required]
        public DateTime FechaHora { get; set; }
        [Required]
        public TipoEvento Tipo { get; set; }
        [Range(10, 300)]
        public string Descripcion { get; set; } = string.Empty;


        [ForeignKey(nameof(Dispositivo))]
        public int DispositivoId { get; set; }
        public Dispositivo Dispositivo { get; set; } = null!;

        [ForeignKey(nameof(Alarma))]
        public int? AlarmaId { get; set; }
        public Alarma Alarma { get; set; } = null!;
    }

    public enum TipoEvento 
    {
        AlarmaActivada,
        DosisEntregada,
        LogError
    }
}