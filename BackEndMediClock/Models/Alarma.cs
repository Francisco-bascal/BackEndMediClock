using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEndMediClock.Models
{
    public class Alarma
    {
        [Key]
        public int AlarmaId { get; set; }
        [Required, Range(1,7, ErrorMessage = "El día de la semana debe ser un valor entre 1 y 7 (Lunes a Domingo)")]
        public int DiaSemana { get; set; }
        [Required, Range(1,3, ErrorMessage = "El número de la alarma debe ser un valor entre 1 y 3 (máximo 3 alarmas al día)")]
        public int NumeroAlarma { get; set; }
        [Required]
        public TimeOnly Hora { get; set; }


        [ForeignKey(nameof(Dispositivo))]
        public int DispositivoId { get; set; }
        public Dispositivo Dispositivo { get; set; } = null!;

        [InverseProperty(nameof(Evento.Alarma))]
        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    }
}