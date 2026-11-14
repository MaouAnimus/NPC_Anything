namespace NpcAnything.Core.Domain;

/// <summary>
/// Tipo de estímulo percibido a través de los sensores del mundo físico.
/// </summary>
public enum StimulusType
{
    Visual,
    Audio,
    Damage,
    SquadMessage
}

/// <summary>
/// Representa un estímulo sensorial recibido por un NPC en un instante de simulación.
/// </summary>
public record Stimulus(
    string SubjectId,
    StimulusType Kind,
    Vec3 EstimatedPosition,
    float Confidence,
    float PrecisionMeters,
    long ObservedAtMs,
    string SourceId)
{
    public float Confidence { get; init; } = Math.Clamp(Confidence, 0f, 1f);
    public float PrecisionMeters { get; init; } = Math.Max(0f, PrecisionMeters);
}
