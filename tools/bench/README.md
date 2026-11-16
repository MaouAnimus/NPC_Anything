# Herramientas de Benchmarking y Telemetría

Este módulo provee utilidades de automatización para la ejecución empírica de escenarios en Godot y la comparación estadística de registros JSONL de telemetría entre la **IA de Referencia (Baseline FSM)** y **NpcAnything.Core**.

---

## 1. `scenario_runner.py` — Ejecutor de Escenarios

Ejecuta escenas de Godot en modo `--headless`, garantizando condiciones iniciales idénticas (semillas fijadas y duración acotada) y recolectando trazas en `runs/`.

### Uso Básico:
```powershell
# Ejecutar la arena con NpcAnything.Core durante 15 segundos:
python tools/bench/scenario_runner.py --scene "res://scenes/Arena_NpcAnything.tscn" --seed 42 --duration 15

# Si Godot .NET no está en el PATH, indicar el binario explícitamente:
python tools/bench/scenario_runner.py --godot-bin "C:\Ruta\Godot_v4.3-stable_mono_win64_console.exe" --compare
```

### Modo Comparativo A/B (`--compare`):
Ejecuta secuencialmente la Variante A (Baseline) y la Variante B (NpcAnything.Core) con la misma semilla y genera automáticamente la tabla comparativa de telemetría.

---

## 2. `compare_telemetry.py` — Comparador de Trazas JSONL

Analiza los eventos estructurados producidos por `ArenaManager` y `GodotTelemetryExporter`:

### Métricas Analizadas:
- **Distribución de acciones:** proporción de tiempo y decisiones en `Patrol`, `Pursue`, `Attack`, `Investigate` y `TakeCover`.
- **Justicia y combate:** disparos efectuados vs. bloqueados por línea de visión u orientación.
- **Entropía táctica ($H$ de Shannon):** variedad y distribución de transiciones de comportamiento.
- **Auditoría de anti-omnisciencia:** detección de cualquier intento de wallhack o ataque sin observación permitida.

### Uso:
```powershell
# Analizar una corrida individual:
python tools/bench/compare_telemetry.py runs/run_baseline.jsonl

# Comparar dos corridas (Variante A vs Variante B):
python tools/bench/compare_telemetry.py runs/run_baseline.jsonl runs/run_core.jsonl --label-a "Baseline FSM" --label-b "NpcAnything Core"

# Exportar métricas agregadas en formato JSON:
python tools/bench/compare_telemetry.py runs/run_baseline.jsonl runs/run_core.jsonl --json
```
