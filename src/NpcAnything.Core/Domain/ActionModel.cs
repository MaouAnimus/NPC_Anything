namespace NpcAnything.Core.Domain;

/// <summary>
/// Acciones de alto nivel disponibles para un NPC según su rol y estado.
/// </summary>
public enum ActionKind
{
    Hold,
    Patrol,
    Investigate,
    Pursue,
    TakeCover,
    Attack,
    Flank,
    Regroup,
    Retreat,
    Follow
}

/// <summary>
/// Candidato de acción evaluado por el selector de utilidad con su puntuación y código de justificación.
/// </summary>
public record ActionCandidate(
    ActionKind Kind,
    float Score,
    string ReasonCode,
    string? TargetId = null,
    Vec3? TargetPosition = null)
{
    public float Score { get; init; } = Math.Clamp(Score, 0f, 1f);
}

/// <summary>
/// Decisión final emitida hacia el puerto ejecutor del motor de juego.
/// </summary>
public record ActionDecision(
    ActionKind Kind,
    float Score,
    string ReasonCode,
    string? TargetId = null,
    Vec3? TargetPosition = null,
    long DecidedAtMs = 0)
{
    public float Score { get; init; } = Math.Clamp(Score, 0f, 1f);

    public static ActionDecision FallbackHold(string reason = "FALLBACK_NO_VALID_ACTION", long timeMs = 0) =>
        new(ActionKind.Hold, 0.0f, reason, DecidedAtMs: timeMs);
}
