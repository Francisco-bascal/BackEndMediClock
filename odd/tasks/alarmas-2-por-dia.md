# Alarmas: 2 por día (14 compartimientos)

## Objetivo

Alinear todo el sistema a **2 alarmas por día = 14 compartimientos** (7 días × 2), y ajustar la
constante de pasos del motor al nuevo paso entre compartimientos.

## Problema

El límite de alarmas quedó desincronizado entre las tres capas:

| Capa | Estado actual | Correcto |
| --- | --- | --- |
| Backend | `Range(1,2)` + índice único `(DispositivoId, DiaSemana, NumeroAlarma)` | ya correcto |
| Frontend | `Range(1,3)` y UI "1 a 3", `max="3"` | 1-2 |
| Firmware | NVS con 21 celdas lógicas (`a{d}s{0..2}*`), `yaDisparo[7][3]`, scheduler `slot < 3` | 14 celdas, `[7][2]`, `slot < 2` |

## Por qué

El usuario define que el diseño físico correcto son 14 compartimientos. Crear la alarma 3
desde la web devuelve 400 hoy, y el firmware reserva una celda por día que ya no existe.

## Alcance autorizado

- `FrontMediclock/` (modelo de alarma y formulario de `Home.razor`).
- `BackEndMediClock/nuevo_mediclock_ino/` (NVS, scheduler, textos y constante de motor).
- **No** se toca el backend: ya cumple la regla de 2.
- **No** se toca `config.h` en credenciales, pines ni red.

## Restricciones y verificación

- No hay `dotnet` ni `arduino-cli` en este host: la verificación es lectura estructural y
  revisión de contrato, no compilación. La compilación queda del lado del usuario.
- TDD: no hay runner ni proyecto de tests en el repo. Modo efectivo: **off** por ausencia de
  runner; verificación funcional = build del usuario + revisión estructural.
- El firmware **no modelaba compartimientos**: cada dosis hacía homing al reed y avanzaba
  `DISPENSAR_PASOS` (273) sin importar la alarma. La identidad `(día, slot)` no llegaba al motor.
  Corregido en T4: ahora el destino es `indiceCompartimiento(dia, slot) * 146` desde el origen.
- `DISPENSAR_PASOS = 273` era un valor **calibrado a mano** (`config.h:88-89`), no derivado de
  `2048 / N`, y no correspondía a ningún paso entre compartimientos. Eliminado en T4.

## Tareas

- [x] **T1 — Frontend: límite 2.** `Modelos/Alarma.cs` (comentario + `Range(1,2)`),
      `Pages/Home.razor` (texto "1 a 2" y `max="2"`). Ruta: inline (2 archivos, cambio mecánico).
      Hecho en `4420b49`. Verificación: grep sin residuos de "1 a 3"/`max="3"`/`Range(1,3)`;
      compilación pendiente del lado del usuario (no hay `dotnet` en este host).
- [x] **T2 — Firmware: esquema 7×2.** Comentarios `slot 0-2`, `yaDisparo[7][2]`, límites
      `slot > 1` en las 3 validaciones, siembra `slot < 2`, scheduler `slot < 2`, textos de
      contrato `numeroAlarma 1-2`, LCD y evento. Ruta: delegada (1 archivo grande, muchas
      ubicaciones acopladas). Hecho en `7671ef5`.
- [x] **T3 — Firmware: migración de NVS.** `inicializarNVSsiVacio()` corta si existe `alarm_init`,
      así que una placa ya usada conserva las celdas `s2` viejas. Agregar marca de versión y
      limpiar las celdas que ya no existen. Ruta: delegada (misma pasada que T2). Hecho en
      `7671ef5`: clave `alarm_ver` (UChar) = 2, borra `a{d}s2{h,m,e}` y siembra lo faltante sin
      pisar horarios existentes. Idempotente.
- [x] **T4 — Firmware: pasos por compartimiento.** Depende de la definición física del usuario.
      Ruta: delegada (misma pasada que T2/T3). Hecho en `6938907`: `indiceCompartimiento()` con
      `((dia + 6) % 7) * 2 + slot`, `PASOS_POR_COMPARTIMIENTO = 146`, se elimina
      `DISPENSAR_PASOS`, homing con margen (2560) y dispensado forzado secuencial de 14 pasos.
      Verificación: harness host sobre las 14 combinaciones; sin compilación ESP32 en este host.

## Decisión de producto registrada

- El forzado es **demostración**: cada pulsación simula la próxima alarma de la semana, avanza un
  compartimiento más desde el origen y vuelve a empezar después de domingo 2 (índice en RAM, se
  reinicia al arrancar). La primera pulsación cae en el origen y no gira.
- **Falla de reed (decisión del usuario, 2026-10-21):** si el homing no encuentra el origen dentro
  de `HOMING_MAX_PASOS`, se dispensa igual para no trabar el equipo. Se acepta el riesgo de
  entregar desde una posición desconocida; queda como mejora futura abortar y reintentar.

## Criterios de aceptación

1. El front no permite ni muestra números de alarma fuera de 1-2.
2. El firmware no lee, escribe ni dispara el slot 2 (tercera alarma).
3. Una placa con NVS vieja queda con 14 celdas útiles y sin residuos `s2` activos.
4. La distancia al compartimiento sale de `índice * PASOS_POR_COMPARTIMIENTO` desde el origen,
   con `146` medido en banco y documentado como derivado de `2048 / 14`.
5. Backend intacto: sigue aceptando 1-2 por día.

## Checks

- Frontend: `dotnet build FrontMediclock/FrontEndMediClock.csproj` (en el host del usuario).
- Firmware: `arduino-cli compile --fqbn esp32:esp32:esp32 nuevo_mediclock_ino` (host del usuario).
- Estructural: sin referencias residuales a 3 slots ni a 21 celdas.

## Estrategia de entrega

Cambio chico (<400 líneas): un work unit por tarea, rama `feature/alarmas-2-por-dia`, commits
conventional. Push y merge quedan a decisión del usuario.
