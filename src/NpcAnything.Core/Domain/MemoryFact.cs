namespace NpcAnything.Core.Domain;

/// <summary>
/// Representa un hecho almacenado en la memoria de trabajo del NPC con vigencia acotada y degradación paulatina.
/// </summary>
public record MemoryFact(
    string SubjectId,
    Vec3 EstimatedPosition,
    float Confidence,
    float PrecisionMeters,
    long LastUpdatedMs,
    long ExpiresAtMs,
    string Provenance)
{
    public float Confidence { get; init; } = Math.Clamp(Confidence, 0f, 1f);
    public float PrecisionMeters { get; init; } = Math.Max(0f, PrecisionMeters);

    /// <summary>
    /// Indica si el hecho ha superado su tiempo de vida máximo (TTL).
    /// </summary>
    public bool IsExpired(long currentSimulationTimeMs) => currentSimulationTimeMs >= ExpiresAtMs;

    /// <summary>
    /// Calcula la confianza degradada en base al tiempo transcurrido desde la última observación directa.
    /// </summary>
    public float GetDecayedConfidence(long currentSimulationTimeMs, float decayPerSecond = 0.15f)
    {
        if (IsExpired(currentSimulationTimeMs)) return 0f;
        long elapsedMs = Math.Max(0, currentSimulationTimeMs - LastUpdatedMs);
        float elapsedSeconds = elapsedMs / 1000f;
        float remainingFactor = MathF.Exp(-decayPerSecond * elapsedSeconds);
        return Math.Clamp(Confidence * remainingFactor, 0f, 1f);
    }

    /// <summary>
    /// Calcula la incertidumbre espacial creciente (metros de error) a medida que pasa el tiempo sin confirmación sensorial.
    /// </summary>
    public float GetUncertaintyMeters(long currentSimulationTimeMs, float expansionMetersPerSecond = 1.0f)
    {
        long elapsedMs = Math.Max(0, currentSimulationTimeMs - LastUpdatedMs);
        float elapsedSeconds = elapsedMs / 1000f;
        return PrecisionMeters + (expansionMetersPerSecond * elapsedSeconds);
    }
}
