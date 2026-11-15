namespace NpcAnything.Core.Domain;

/// <summary>
/// Contexto local y autorizado de un NPC.
/// IMPORTANTE: No contiene datos omniscientes del mundo, solo lo percibido y recordado.
/// </summary>
public record NpcContext(
    string NpcId,
    NpcRole Role,
    Vec3 Position,
    float HealthRatio,
    ActionKind CurrentAction,
    float CurrentActionScore,
    long CurrentTimeMs,
    IReadOnlyDictionary<string, MemoryFact> Memory,
    IReadOnlyList<Vec3> ReachableCoverPoints,
    IReadOnlySet<string> ReservedSquadCovers,
    IReadOnlyList<Vec3>? PatrolWaypoints = null)
{
    public float HealthRatio { get; init; } = Math.Clamp(HealthRatio, 0f, 1f);
    public IReadOnlyList<Vec3> PatrolWaypoints { get; init; } = PatrolWaypoints ?? Array.Empty<Vec3>();
}
