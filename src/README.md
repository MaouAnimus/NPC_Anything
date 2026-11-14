# Código Fuente (Source Code)

Estructura modular de dos capas según `docs/03_ARQUITECTURA.md`:

- **`NpcAnything.Core/`**:
  - Biblioteca de clases en C# pura (agnóstica al motor).
  - Define contratos, entidades de dominio (`Stimulus`, `MemoryFact`, `ActionCandidate`, `ActionDecision`), puertos de entrada/salida (`IWorldSensorPort`, `IActionExecutorPort`), evaluador de utilidad, reglas de justicia y coordinación de escuadrón.
  - **Regla estricta:** Cero dependencias de Godot, Unity ni ningún motor gráfico.

- **`NpcAnything.Godot/`**:
  - Adaptador de integración para Godot 4.x con .NET / C#.
  - Implementa los puertos conectando sensores de Godot (RayCast3D, Area3D, navegación NavigationServer3D) con el núcleo de `NpcAnything.Core`.
  - Emite telemetría y eventos de depuración visual (overlay).
