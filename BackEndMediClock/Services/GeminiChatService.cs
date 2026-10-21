using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BackEndMediClock.DTOs;
using BackEndMediClock.Common;

namespace BackEndMediClock.Services
{
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuracion;
        private readonly AlarmaService _alarmaService;
        private readonly DispositivoService _dispositivoService;
        private readonly ILogger<GeminiChatService> _logger;

        private const string InstruccionSistema = 
@"Eres 'MediClock Bot', el asistente inteligente integrado del pastillero automatizado MediClock.

INFORMACIÓN DEL SISTEMA MEDICLOCK:
- Es un dispensador de pastillas automatizado controlado por un microcontrolador ESP32 conectado por WiFi y WebSocket en tiempo real con este servidor.
- Mecanismo mecánico: Utiliza un motor paso a paso 28BYJ-48 con driver ULN2003 que hace girar una ranura helicoidal (273 pasos por dosis para dispensar una pastilla).
- Sensores y calibración: Cuenta con un sensor magnético Reed en GPIO34 para detectar la posición de inicio (homing) al calibrar la rotación.
- Señales audiovisuales: Posee un buzzer pasivo con modulación PWM (frecuencia 2000 Hz) y un LED indicador en GPIO23 que alertan visual y sonoramente al usuario cuando es momento de tomar la medicación.
- Pantalla y hora: Pantalla LCD 16x2 por I2C que muestra la hora actual y mensajes del estado, y un reloj de tiempo real RTC DS1302 sincronizado automáticamente vía protocolo NTP (hora de Argentina UTC-3).
- Botón de pánico/físico: GPIO35 permite dispensación o alerta física directa sin depender de la red.
- Sincronización en tiempo real: Cualquier cambio en las alarmas notifica al ESP32 al instante mediante el comando WebSocket REFRESH_CONFIG.

REGLAS DE NEGOCIO PARA ALARMAS:
1. Días de la semana: Se representan numéricamente del 1 al 7 (1 = Lunes, 2 = Martes, 3 = Miércoles, 4 = Jueves, 5 = Viernes, 6 = Sábado, 7 = Domingo).
2. Cantidad de alarmas: Máximo 2 alarmas por día por dispositivo (número 1 y número 2).
3. Horarios: Formato de 24 horas 'HH:mm'. Si existen dos alarmas para un mismo día, la alarma 1 DEBE ser obligatoriamente anterior a la alarma 2.
4. Historial: Las alarmas pueden ser eliminadas libremente; los eventos pasados se preservan.

TUS CAPACIDADES:
- Responder preguntas del usuario sobre cómo funciona el pastillero, sus componentes y estados.
- Cuando el usuario te pida crear, modificar, consultar o borrar alarmas, o dispensar pastillas, invoca las herramientas (funciones) correspondientes.
- Sé claro, amable, conciso y empático con el usuario o paciente. Responde siempre en español.";

        public GeminiChatService(
            HttpClient httpClient,
            IConfiguration configuracion,
            AlarmaService alarmaService,
            DispositivoService dispositivoService,
            ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _configuracion = configuracion;
            _alarmaService = alarmaService;
            _dispositivoService = dispositivoService;
            _logger = logger;
        }

        public async Task<Resultado<RespuestaChatDto>> ProcesarMensajeAsync(
            SolicitudChatDto solicitud,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuracion["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Resultado<RespuestaChatDto>.Validacion("La clave de API de Google Gemini no está configurada en el servidor.");
            }

            var modeloConfigurado = _configuracion["Gemini:Modelo"] ?? "gemini-3.5-flash-lite";
            var modelosCandidatos = new[] { modeloConfigurado, "gemini-3.5-flash-lite", "gemini-3.5-flash", "gemini-flash-lite-latest", "gemini-3.6-flash" }
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var jsonContents = new JsonArray();

            // Historial previo
            if (solicitud.Historial != null)
            {
                foreach (var mensaje in solicitud.Historial)
                {
                    var rol = mensaje.Rol.Equals("asistente", StringComparison.OrdinalIgnoreCase) ||
                              mensaje.Rol.Equals("model", StringComparison.OrdinalIgnoreCase)
                        ? "model"
                        : "user";

                    jsonContents.Add(new JsonObject
                    {
                        ["role"] = rol,
                        ["parts"] = new JsonArray
                        {
                            new JsonObject { ["text"] = mensaje.Contenido }
                        }
                    });
                }
            }

            // Mensaje actual del usuario
            var contextoUsuario = $"[Dispositivo actual ID: {solicitud.DispositivoId}] Mensaje del usuario: {solicitud.Mensaje}";
            jsonContents.Add(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray
                {
                    new JsonObject { ["text"] = contextoUsuario }
                }
            });

            bool accionEjecutada = false;
            string? detalleAccion = null;
            int iteraciones = 0;
            const int maxIteraciones = 5;

            while (iteraciones < maxIteraciones)
            {
                iteraciones++;

                var cuerpoPeticion = new JsonObject
                {
                    ["systemInstruction"] = new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject { ["text"] = InstruccionSistema }
                        }
                    },
                    ["contents"] = jsonContents.DeepClone(),
                    ["tools"] = ObtenerDeclaracionHerramientas()
                };

                HttpResponseMessage? respuestaHttp = null;
                string ultimoError = string.Empty;

                foreach (var modeloActual in modelosCandidatos)
                {
                    var urlEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modeloActual}:generateContent?key={apiKey}";
                    try
                    {
                        respuestaHttp = await _httpClient.PostAsJsonAsync(urlEndpoint, cuerpoPeticion, cancellationToken);
                        if (respuestaHttp.IsSuccessStatusCode)
                        {
                            break;
                        }

                        ultimoError = await respuestaHttp.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Modelo {Modelo} retornó {Codigo}: {Detalle}", modeloActual, respuestaHttp.StatusCode, ultimoError);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al comunicar con la API de Gemini usando modelo {Modelo}.", modeloActual);
                        ultimoError = ex.Message;
                    }
                }

                if (respuestaHttp == null || !respuestaHttp.IsSuccessStatusCode)
                {
                    return Resultado<RespuestaChatDto>.Validacion($"El servicio de IA no pudo procesar la solicitud: {ultimoError}");
                }

                var nodoRespuesta = await respuestaHttp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
                var candidato = nodoRespuesta?["candidates"]?[0];
                var contenidoModelo = candidato?["content"];
                var partes = contenidoModelo?["parts"]?.AsArray();

                if (contenidoModelo is null || partes is null || partes.Count == 0)
                {
                    return Resultado<RespuestaChatDto>.Success(new RespuestaChatDto
                    {
                        Respuesta = "No se recibió respuesta del asistente.",
                        AccionEjecutada = accionEjecutada,
                        DetalleAccion = detalleAccion
                    });
                }

                // Verificar si Gemini incluyó una llamada a herramienta (functionCall)
                JsonNode? llamadaFuncion = null;
                foreach (var parte in partes)
                {
                    if (parte?["functionCall"] != null)
                    {
                        llamadaFuncion = parte["functionCall"];
                        break;
                    }
                }

                if (llamadaFuncion != null)
                {
                    var nombreFuncion = llamadaFuncion["name"]?.ToString() ?? string.Empty;
                    var argumentos = llamadaFuncion["args"] as JsonObject ?? new JsonObject();

                    // Guardamos el contenido completo del modelo en el historial (incluyendo firmas/tokens de razonamiento)
                    jsonContents.Add(contenidoModelo.DeepClone());

                    // Ejecutar la acción del sistema
                    var resultadoEjecucion = await EjecutarHerramientaAsync(
                        nombreFuncion,
                        argumentos,
                        solicitud.DispositivoId,
                        cancellationToken);

                    accionEjecutada = true;
                    detalleAccion = $"{nombreFuncion}: {resultadoEjecucion}";

                    // Enviar el resultado de vuelta a Gemini como rol "user" con functionResponse
                    jsonContents.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["functionResponse"] = new JsonObject
                                {
                                    ["name"] = nombreFuncion,
                                    ["response"] = new JsonObject
                                    {
                                        ["output"] = resultadoEjecucion
                                    }
                                }
                            }
                        }
                    });

                    // Continúa el ciclo para que Gemini redacte la respuesta final
                    continue;
                }

                // Respuesta de texto estándar: concatenar partes de texto
                var textoBuilder = new System.Text.StringBuilder();
                foreach (var parte in partes)
                {
                    var txt = parte?["text"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        if (textoBuilder.Length > 0)
                        {
                            textoBuilder.AppendLine();
                        }
                        textoBuilder.Append(txt);
                    }
                }

                var textoFinal = textoBuilder.ToString();

                return Resultado<RespuestaChatDto>.Success(new RespuestaChatDto
                {
                    Respuesta = textoFinal,
                    AccionEjecutada = accionEjecutada,
                    DetalleAccion = detalleAccion
                });
            }

            return Resultado<RespuestaChatDto>.Success(new RespuestaChatDto
            {
                Respuesta = "Se procesaron las operaciones solicitadas en el pastillero.",
                AccionEjecutada = accionEjecutada,
                DetalleAccion = detalleAccion
            });
        }

        private async Task<string> EjecutarHerramientaAsync(
            string nombreFuncion,
            JsonObject argumentos,
            int dispositivoIdPorDefecto,
            CancellationToken cancellationToken)
        {
            try
            {
                int dispositivoId = argumentos["dispositivoId"]?.GetValue<int>() ?? dispositivoIdPorDefecto;

                switch (nombreFuncion)
                {
                    case "listar_alarmas":
                    {
                        var res = await _alarmaService.ObtenerAlarmasPorDispositivoAsync(dispositivoId, cancellationToken);
                        if (!res.IsValid || res.Value == null)
                        {
                            return $"Error al obtener alarmas: {string.Join(", ", res.Errors)}";
                        }

                        if (!res.Value.Any())
                        {
                            return "Actualmente no hay alarmas programadas en el dispositivo.";
                        }

                        var lineas = res.Value.Select(a =>
                            $"ID: {a.AlarmaId}, Día: {ObtenerNombreDia(a.DiaSemana)} (día {a.DiaSemana}), Alarma Nro: {a.NumeroAlarma}, Hora: {a.Hora:HH\\:mm}");
                        return "Alarmas actuales:\n" + string.Join("\n", lineas);
                    }

                    case "crear_alarma":
                    {
                        var diaSemana = argumentos["diaSemana"]?.GetValue<int>() ?? 1;
                        var numeroAlarma = argumentos["numeroAlarma"]?.GetValue<int>() ?? 1;
                        var horaStr = argumentos["hora"]?.ToString() ?? "08:00";

                        if (!TimeOnly.TryParse(horaStr, out var hora))
                        {
                            return $"Formato de hora inválido: '{horaStr}'. Debe ser HH:mm.";
                        }

                        var dto = new CreateAlarmaDto
                        {
                            DiaSemana = diaSemana,
                            NumeroAlarma = numeroAlarma,
                            Hora = hora
                        };

                        var res = await _alarmaService.CrearAsync(dispositivoId, dto, cancellationToken);
                        if (!res.IsValid)
                        {
                            return $"No se pudo crear la alarma: {string.Join(", ", res.Errors)}";
                        }

                        return $"Alarma creada con éxito: Día {ObtenerNombreDia(dto.DiaSemana)}, Alarma {dto.NumeroAlarma} a las {dto.Hora:HH\\:mm}. El pastillero ya fue notificado.";
                    }

                    case "actualizar_alarma":
                    {
                        var alarmaId = argumentos["alarmaId"]?.GetValue<int>() ?? 0;
                        var diaSemana = argumentos["diaSemana"]?.GetValue<int>() ?? 1;
                        var numeroAlarma = argumentos["numeroAlarma"]?.GetValue<int>() ?? 1;
                        var horaStr = argumentos["hora"]?.ToString() ?? "08:00";

                        if (!TimeOnly.TryParse(horaStr, out var hora))
                        {
                            return $"Formato de hora inválido: '{horaStr}'. Debe ser HH:mm.";
                        }

                        var dto = new UpdateAlarmaDto
                        {
                            DiaSemana = diaSemana,
                            NumeroAlarma = numeroAlarma,
                            Hora = hora
                        };

                        var res = await _alarmaService.ActualizarAsync(dispositivoId, alarmaId, dto, cancellationToken);
                        if (!res.IsValid)
                        {
                            return $"No se pudo actualizar la alarma {alarmaId}: {string.Join(", ", res.Errors)}";
                        }

                        return $"Alarma {alarmaId} actualizada exitosamente a: {ObtenerNombreDia(dto.DiaSemana)} a las {dto.Hora:HH\\:mm}. Pastillero sincronizado.";
                    }

                    case "eliminar_alarma":
                    {
                        var alarmaId = argumentos["alarmaId"]?.GetValue<int>() ?? 0;
                        var res = await _alarmaService.EliminarAsync(dispositivoId, alarmaId, cancellationToken);
                        if (!res.IsValid)
                        {
                            return $"No se pudo eliminar la alarma {alarmaId}: {string.Join(", ", res.Errors)}";
                        }

                        return $"Alarma {alarmaId} eliminada correctamente. El pastillero actualizó su configuración.";
                    }

                    case "forzar_dispensacion":
                    {
                        var res = await _dispositivoService.SolicitarDispensacionAsync(dispositivoId, cancellationToken);
                        if (!res.IsValid)
                        {
                            return $"No se pudo solicitar la dispensación: {string.Join(", ", res.Errors)}";
                        }

                        return "Comando de dispensación enviado con éxito. El motor del pastillero girará la ranura helicoidal para entregar la dosis.";
                    }

                    case "consultar_estado_dispositivo":
                    {
                        var res = await _dispositivoService.ObtenerPorIdAsync(dispositivoId, cancellationToken);
                        if (!res.IsValid || res.Value == null)
                        {
                            return $"No se encontró el dispositivo {dispositivoId}.";
                        }

                        return $"Dispositivo '{res.Value.Nombre}' (ID: {res.Value.DispositivoId}) conectado y activo.";
                    }

                    default:
                        return $"La herramienta '{nombreFuncion}' no está implementada.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar herramienta {Funcion}", nombreFuncion);
                return $"Error de ejecución en {nombreFuncion}: {ex.Message}";
            }
        }

        private static string ObtenerNombreDia(int dia) => dia switch
        {
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            7 => "Domingo",
            _ => $"Día {dia}"
        };

        private static JsonArray ObtenerDeclaracionHerramientas()
        {
            return new JsonArray
            {
                new JsonObject
                {
                    ["functionDeclarations"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "listar_alarmas",
                            ["description"] = "Obtiene la lista completa de alarmas programadas en el pastillero.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo pastillero"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "crear_alarma",
                            ["description"] = "Crea una nueva alarma en el pastillero. Reglas: DiaSemana de 1 a 7 (1=Lunes, 7=Domingo). NumeroAlarma 1 o 2 (máximo 2 por día). Si existen dos alarmas para el mismo día, la alarma 1 debe ser más temprana que la 2.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo"
                                    },
                                    ["diaSemana"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Día de la semana: 1=Lunes, 2=Martes, 3=Miércoles, 4=Jueves, 5=Viernes, 6=Sábado, 7=Domingo"
                                    },
                                    ["numeroAlarma"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Número de alarma para el día (1 para la primera toma, 2 para la segunda toma)"
                                    },
                                    ["hora"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Hora en formato 24 horas 'HH:mm' (por ejemplo '08:30' o '21:00')"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId", "diaSemana", "numeroAlarma", "hora" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "actualizar_alarma",
                            ["description"] = "Actualiza el día, número u hora de una alarma existente identificada por su ID.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo"
                                    },
                                    ["alarmaId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID de la alarma a modificar"
                                    },
                                    ["diaSemana"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Día de la semana (1 a 7)"
                                    },
                                    ["numeroAlarma"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Número de alarma (1 o 2)"
                                    },
                                    ["hora"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Nueva hora en formato 'HH:mm'"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId", "alarmaId", "diaSemana", "numeroAlarma", "hora" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "eliminar_alarma",
                            ["description"] = "Elimina una alarma configurada del pastillero a partir de su ID.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo"
                                    },
                                    ["alarmaId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID numérico de la alarma a eliminar"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId", "alarmaId" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "forzar_dispensacion",
                            ["description"] = "Ordena inmediatamente al pastillero hacer girar el motor para dispensar una dosis.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "consultar_estado_dispositivo",
                            ["description"] = "Consulta la información y estado actual del pastillero.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["dispositivoId"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "ID del dispositivo"
                                    }
                                },
                                ["required"] = new JsonArray { "dispositivoId" }
                            }
                        }
                    }
                }
            };
        }
    }
}
