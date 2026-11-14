# 11. Contratos de interfaces y eventos (borrador técnico)

## Principios de API

Las interfaces pertenecen al núcleo agnóstico del motor; los adaptadores dependen del Core y no al revés. Se priorizan tipos inmutables, configuración validable, fuentes de conocimiento trazables y decisiones puras cuando es factible. El siguiente código es **ilustrativo**; su compilación e integración requieren implementación y validación.

## Modelo de dominio en C# (especificación inicial)

```csharp
public readonly record struct Vec3(float X, float Y, float Z);

public enum StimulusType { Visual, Audio, Damage, SquadMessage }
public enum ActionKind { Patrol, Investigate, TakeCover, Attack, Flank, Regroup, Retreat, Follow, Hold }

public record Stimulus(
    string SubjectId,
    StimulusType Kind,
    Vec3 EstimatedPosition,
    float Confidence,
    float PrecisionMeters,
    long ObservedAtMs,
    string SourceId);

public record MemoryFact(
    string SubjectId,
    Vec3 EstimatedPosition,
    float Confidence,
    float PrecisionMeters,
    long LastUpdatedMs,
    long ExpiresAtMs,
    string Provenance);

public record ActionCandidate(ActionKind Kind, float Score, string ReasonCode);
public record ActionDecision(ActionKind Kind, float Score, string ReasonCode, string? TargetId);

public interface IWorldSensorPort
{
    IReadOnlyList<Stimulus> Observe(string npcId, long simulationTimeMs);
}
public interface IActionExecutorPort
{
    bool CanExecute(string npcId, ActionKind action);
    void Request(string npcId, ActionDecision decision);
    void Cancel(string npcId, string reason);
}
public interface IDecisionPolicy
{
    ActionDecision Decide(NpcContext context, IReadOnlyList<ActionCandidate> candidates);
}
```

`NpcContext` es un DTO aún por diseñar con memoria permitida, estado propio y rol; no contiene estado omnisciente del mundo. `TargetId` no implica que el objetivo sea visible: una acción dirigida a una posición requiere evidencia y vigencia.

## Evento JSONL: esquema conceptual

```json
{
  "schema_version": "0.1",
  "run_id": "demo_run_001",
  "scenario_id": "SC-02",
  "seed": 42,
  "simulation_time_ms": 12500,
  "npc_id": "enemy_02",
  "event": "action_selected",
  "role": "flanker",
  "action": "TakeCover",
  "score": 0.73,
  "reason_code": "THREAT_HIGH_COVER_REACHABLE",
  "knowledge_source": "visual_last_seen",
  "knowledge_age_ms": 2200,
  "profile_hash": "sha256:example-not-a-real-hash"
}
```

El hash anterior es marcador ilustrativo, no una huella válida. Las herramientas reales deben calcular SHA-256 de la configuración normalizada.

## Reglas de validación

- `Confidence` ∈ [0,1]; `PrecisionMeters` ≥ 0.
- `ObservedAtMs` ≤ tiempo actual y `ExpiresAtMs` ≥ `LastUpdatedMs`.
- `ReasonCode` pertenece a catálogo versionado.
- `ActionKind` es un enum cerrado por versión.
- No persistir rutas locales absolutas en logs públicos.
- Mensajes de escuadrón identifican emisor, canal, destino, TTL, prioridad y confianza.

## Contratos para herramienta de análisis local

```json
{
  "tool": "inspect_project",
  "arguments": {
    "workspace_relative_path": ".",
    "max_files": 1000,
    "include_binary_metadata": false
  },
  "result": {
    "engine": "Godot",
    "engine_confidence": "high",
    "evidence": ["project.godot"],
    "warnings": []
  }
}
```

**Contrato de seguridad:** la herramienta recibe rutas relativas normalizadas dentro del workspace configurado por operador. Las aplicaciones no deben confiar ciegamente en el ejemplo de `engine_confidence`; debe derivarse de evidencia verificable. Rechazar symlinks que resuelvan fuera del workspace.

## Versionado y compatibilidad

- Cambios de campo obligatorios o comportamiento incompatible -> incremento de versión mayor del schema.
- Campos opcionales nuevos -> versión menor y lectores tolerantes cuando sea seguro.
- Los tests de serialización mantienen fixtures estables.
- La demo debe registrar `build`, `schema_version` y `profile_hash` para permitir comparación histórica.
