namespace BackEndMediClock.DTOs
{
    public class ConfiguracionDispositivoDto
    {
        public int DispositivoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public List<AlarmaDto> Alarmas { get; set; } = new List<AlarmaDto>();
    }
}