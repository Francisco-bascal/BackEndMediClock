using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEndMediClock.Models
{
    public class Dispositivo
    {
        [Key]
        public int DispositivoId { get; set; }
        [Required, Range(4, 50, ErrorMessage = "El nombre de la alarma debe tener entre 4 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;


        [InverseProperty(nameof(Alarma.Dispositivo))]
        public ICollection<Alarma> Alarmas { get; set; } = new List<Alarma>();

        [InverseProperty(nameof(Evento.Dispositivo))]
        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    }
}