# Herramientas de Ingeniería y Benchmarking

- **`agent/`**:
  - Prototipo del agente de ingeniería local en Python (inspirado en la metodología de [Co-op Anything](https://github.com/cranebeaker/coop-anything)).
  - Implementa herramientas de inspección estricta de solo lectura (estilo MCP):
    - `inspect_project`
    - `detect_engine`
    - `find_ai_entrypoints`
    - `list_behaviors`
    - `assess_modifiability`
  - Generación de recomendaciones y diffs seguros en `proposals/` sin tocar archivos originales del usuario sin aprobación.

- **`bench/`**:
  - Runners de simulación, comparador de métricas JSONL y generador de informes de rendimiento (latencia de decisión ms, dispersión espacial, justicia percibida).
  - `python tools/bench/run_scenarios.py --godot <Godot-4.3-.NET>` ejecuta en modo headless el fixture `Arena_TransitionTest.tscn` para INT-01 a INT-07, usa semillas consecutivas desde 42 y valida el JSONL real contra los cambios de acción `Patrol` → `Pursue` → `Attack`. El jugador comienza fuera del FOV; la patrulla existente lo adquiere al girar y permite comprobar la transición sin introducir una ruta de movimiento nueva. Los archivos se conservan bajo `runs/int-scenarios/`.
  - Para validar el parser sin Godot: `python -m unittest discover -s tools/bench -p "test_*.py"`.
  - INT-01 a INT-07 aún no tienen definiciones de estímulo distintas en el backlog/documentación; por ahora son etiquetas de ejecución del mismo fixture de transición, no siete escenarios funcionalmente diferentes.
