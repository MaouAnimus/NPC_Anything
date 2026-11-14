# 15. Sprint 1 — instrucciones de ejecución (dos primeras semanas)

## Objetivo de sprint

Construir un escenario reproducible que establezca el punto de comparación. **No** comenzar por integrar un LLM ni buscar un juego comercial complejo. El primer resultado útil es un baseline honesto y una arquitectura que pueda probarse sin engine.

## Preparación

1. Confirmar tiempo semanal, responsable y editor/IDE.
2. Seleccionar una versión **estable** y compatible de Godot .NET y SDK .NET; anotar versiones exactas y fuente oficial.
3. Crear repo Git, `.gitignore`, `README`, licencia provisional solo si se han aclarado dependencias, `docs/`, `demo/`, `src/`, `tests/`, `tools/`, `proposals/`.
4. Abrir issues `BK-001` a `BK-005` y crear board con `Todo / In Progress / Review / Done`.
5. Fijar convenciones de código, versiones, nombres de escenario, seed y formato de logs.

## Estructura propuesta

```text
npc-anything/
├── README.md
├── LICENSE                 # solo tras revisar licencia real
├── docs/
├── src/
│   ├── NpcAnything.Core/
│   └── NpcAnything.Godot/
├── demo/
│   └── GodotArena/
├── tests/
│   ├── Core.Tests/
│   └── Integration/
├── tools/
│   ├── agent/
│   └── bench/
├── scenarios/
├── configs/
├── proposals/              # diffs generados, no aplicar automáticamente
└── .github/workflows/
```

## Paso 1 — mapa controlado

Arena pequeña con tres zonas de cobertura, dos rutas, pared que ocluye visión y un corredor estrecho. Preferir geometría simple, iluminación constante y posiciones etiquetadas. Crear escena de jugador con movimiento básico y un NPC de referencia. Guardar parámetros fijos en archivo, no en código disperso.

## Paso 2 — baseline de comportamiento

FSM mínima: `Patrol -> Chase -> Attack -> Search -> Patrol`. El propósito no es que sea buena: debe ser una representación reproducible y explícita. Capturar un video corto o secuencia de eventos. Asegurar que el baseline no conozca la ubicación del jugador tras oclusión salvo por último avistamiento.

## Paso 3 — contratos de software

Crear tipos `Stimulus`, `MemoryFact`, `ActionCandidate`, `ActionDecision` y puertos de entrada/salida descritos en `11_API_Y_CONTRATOS.md`. Compilar `Core` sin referencias a Godot. Escribir tests para valores fuera de rango y un selector vacío que deba producir fallback.

## Paso 4 — instrumentación mínima

Registrar `run_id`, `scenario_id`, `seed`, `build`, `npc_id`, `simulation_time_ms`, estímulo, estado y acción. Guardar el archivo en carpeta de prueba por sesión, excluida de Git cuando incluya datos de entorno.

## Paso 5 — primeras pruebas

- NPC no detecta al jugador tras obstáculo opaco.
- Una decisión sin acciones válidas no provoca excepción ni loop.
- Mismo estado inicial + semilla genera mismo resultado lógico en test puro.
- Escena se ejecuta en ambiente limpio según README.

## Entregables y revisión de sprint

- Build reproducible de arena con NPC básico.
- Mapa de arquitectura actualizado y ADR-001 decidido.
- `BK-001..005` cerrados cuando corresponda.
- Informe corto: qué funcionó, qué falló, horas invertidas, riesgos y plan siguiente.
- Gate G0: otra persona o un checkout limpio puede seguir guía y abrir demo.

## Comandos orientativos, no ejecutados

```bash
# Si el repositorio ya está creado y el SDK .NET compatible está instalado:
dotnet --info
dotnet test
# Para Godot, utilizar el editor .NET o el ejecutable Godot correspondiente a tu SO.
```

La presencia de un comando en este documento **no significa que se haya probado**. Versionar scripts de automatización una vez se confirme el entorno local.
