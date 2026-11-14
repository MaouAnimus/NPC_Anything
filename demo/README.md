# Demos y Escenarios

Directorio destinado al proyecto de demostración en Godot 4.x:

- **`GodotArena/`**:
  - Escenario controlado de prueba (arena de combate pequeña con tres zonas de cobertura, dos rutas de flanqueo y oclusores de visión).
  - Incluye:
    - Jugador con movimiento y control básico.
    - NPC de referencia (baseline FSM estándar: Patrol -> Chase -> Attack -> Search) para comparar objetivamente contra la IA de NPC Anything.
    - Escena con 3 enemigos coordinados y 1 compañero de apoyo.
    - Exportador de telemetría a JSONL para evaluación repetible.
