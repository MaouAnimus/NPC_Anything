using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Rules;

/// <summary>
/// Filtro de justicia para garantizar que ninguna acción viole las restricciones
/// de información acotada (evitar wallhacks, teletransportación cognitiva o colisión de coberturas).
/// </summary>
public static class FairnessFilter
{
    public const float MinConfidenceToAttack = 0.20f;

    /// <summary>
    /// Filtra y ajusta los candidatos de acción garantizando el cumplimiento de las reglas de juego justo.
    /// </summary>
    public static IReadOnlyList<ActionCandidate> ApplyFairnessRules(
        IEnumerable<ActionCandidate> rawCandidates,
        NpcContext context)
    {
        var result = new List<ActionCandidate>();

        foreach (var candidate in rawCandidates)
        {
            // Regla 1: No atacar a ciegas si no hay hecho en memoria o si la confianza expiró
            if (candidate.Kind == ActionKind.Attack && candidate.TargetId != null)
            {
                if (!context.Memory.TryGetValue(candidate.TargetId, out var fact) ||
                    fact.IsExpired(context.CurrentTimeMs) ||
                    fact.GetDecayedConfidence(context.CurrentTimeMs) < MinConfidenceToAttack)
                {
                    // Se descarta el ataque omnisciente
                    continue;
                }
            }

            // Regla 2: No seleccionar cobertura si está en la lista de reservas activas del escuadrón
            if (candidate.Kind == ActionKind.TakeCover && candidate.TargetPosition.HasValue)
            {
                string coverKey = $"{candidate.TargetPosition.Value.X:F1}_{candidate.TargetPosition.Value.Z:F1}";
                if (context.ReservedSquadCovers.Contains(coverKey))
                {
                    // Cobertura ocupada por aliado
                    continue;
                }
            }

            // Regla 3: Si la acción es investigar, debe tener una posición objetivo válida
            if (candidate.Kind == ActionKind.Investigate && !candidate.TargetPosition.HasValue)
            {
                continue;
            }

            result.Add(candidate);
        }

        return result;
    }
}
