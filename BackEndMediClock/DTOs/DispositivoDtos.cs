using System.ComponentModel.DataAnnotations;

namespace BackEndMediClock.DTOs
{
    public class DispositivoDto
    {
        public int DispositivoId { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }

    public class CreateDispositivoDto
    {
        [Required, StringLength(50, MinimumLength = 4, ErrorMessage = "El nombre del dispositivo debe tener entre 4 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;
    }

    public class UpdateDispositivoDto
    {
        [Required, StringLength(50, MinimumLength = 4, ErrorMessage = "El nombre del dispositivo debe tener entre 4 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;
    }
}