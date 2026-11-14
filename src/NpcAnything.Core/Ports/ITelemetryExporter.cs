using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Evento de telemetría normalizado según el esquema JSONL de docs/11_API_Y_CONTRATOS.md.
/// </summary>
public record TelemetryEvent(
    string SchemaVersion,
    string RunId,
    string ScenarioId,
    int Seed,
    long SimulationTimeMs,
    string NpcId,
    string Event,
    string Role,
    string Action,
    float Score,
    string ReasonCode,
    string KnowledgeSource,
    long KnowledgeAgeMs,
    string ProfileHash);

/// <summary>
/// Puerto para registrar trazas deterministas de decisión y evaluación para QA y benchmarking.
/// </summary>
public interface ITelemetryExporter
{
    void Emit(TelemetryEvent evt);
}
