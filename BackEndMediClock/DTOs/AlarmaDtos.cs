using System.ComponentModel.DataAnnotations;

namespace BackEndMediClock.DTOs
{
    public class AlarmaDto
    {
        public int AlarmaId { get; set; }

        public int DiaSemana { get; set; }

        public int NumeroAlarma { get; set; }

        public TimeOnly Hora { get; set; }

        public int DispositivoId { get; set; }
    }

    public class CreateAlarmaDto
    {
        [Required, Range(1, 7, ErrorMessage = "El día de la semana debe ser un valor entre 1 y 7 (Lunes a Domingo)")]
        public int DiaSemana { get; set; }

        [Required, Range(1, 3, ErrorMessage = "El número de la alarma debe ser un valor entre 1 y 3 (máximo 3 alarmas al día)")]
        public int NumeroAlarma { get; set; }

        [Required]
        public TimeOnly Hora { get; set; }
    }

    public class UpdateAlarmaDto
    {
        [Required, Range(1, 7, ErrorMessage = "El día de la semana debe ser un valor entre 1 y 7 (Lunes a Domingo)")]
        public int DiaSemana { get; set; }

        [Required, Range(1, 3, ErrorMessage = "El número de la alarma debe ser un valor entre 1 y 3 (máximo 3 alarmas al día)")]
        public int NumeroAlarma { get; set; }

        [Required]
        public TimeOnly Hora { get; set; }
    }
}