namespace FrontEndMediClock.Modelos;

public class MensajeChat
{
    public string Rol { get; set; } = "user"; // "user" o "model"
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;
}

public class SolicitudChat
{
    public int DispositivoId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<MensajeChat> Historial { get; set; } = new();
}

public class RespuestaChat
{
    public string Respuesta { get; set; } = string.Empty;
    public bool AccionEjecutada { get; set; }
    public string? DetalleAccion { get; set; }
}
