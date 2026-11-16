using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Rules;

/// <summary>
/// Filtro de justicia para garantizar que ninguna acción viole las restricciones
/// de información acotada (evitar wallhacks, daño a través de obstáculos o colisión de coberturas).
/// </summary>
public static class FairnessFilter
{
    public const float MinConfidenceToAttack = 0.70f;

    /// <summary>
    /// Filtra y descarta candidatos de acción que violen las reglas de juego limpio y no-omnisciencia.
    /// </summary>
    public static IReadOnlyList<ActionCandidate> ApplyFairnessRules(
        IEnumerable<ActionCandidate> rawCandidates,
        NpcContext context)
    {
        var result = new List<ActionCandidate>();

        foreach (var candidate in rawCandidates)
        {
            // Regla 1: Un ataque directo REQUIERE contacto visual comprobado y confianza alta.
            // Los estímulos auditivos o desactualizados NUNCA pueden habilitar ataque directo.
            if (candidate.Kind == ActionKind.Attack && candidate.TargetId != null)
            {
                if (!context.Memory.TryGetValue(candidate.TargetId, out var fact) ||
                    fact.IsExpired(context.CurrentTimeMs) ||
                    fact.GetDecayedConfidence(context.CurrentTimeMs) < MinConfidenceToAttack ||
                    !fact.Provenance.Contains("vision", StringComparison.OrdinalIgnoreCase))
                {
                    // Descartado: Intento de ataque a ciegas o a través de oclusión (anti-wallhack)
                    continue;
                }
            }

            // Regla 2: No seleccionar cobertura si está en la lista de reservas activas del escuadrón
            if (candidate.Kind == ActionKind.TakeCover && candidate.TargetPosition.HasValue)
            {
                string coverKey = $"{candidate.TargetPosition.Value.X:F1}_{candidate.TargetPosition.Value.Z:F1}";
                if (context.ReservedSquadCovers.Contains(coverKey))
                {
                    // Descartado: Cobertura ocupada por un aliado
                    continue;
                }
            }

            // Regla 3: Si la acción es flanquear, debe tener posición válida y no estar reservada por un aliado
            if (candidate.Kind == ActionKind.Flank && candidate.TargetPosition.HasValue)
            {
                string coverKey = $"{candidate.TargetPosition.Value.X:F1}_{candidate.TargetPosition.Value.Z:F1}";
                if (context.ReservedSquadCovers.Contains(coverKey))
                {
                    // Descartado: Ruta de flanqueo reservada por un aliado
                    continue;
                }
            }

            // Regla 4: Si la acción es investigar, debe tener una posición objetivo válida
            if (candidate.Kind == ActionKind.Investigate && !candidate.TargetPosition.HasValue)
            {
                continue;
            }

            // Regla 5: Perseguir (Pursue) requiere una posición objetivo y contacto visual en memoria
            if (candidate.Kind == ActionKind.Pursue)
            {
                if (!candidate.TargetPosition.HasValue)
                {
                    continue;
                }

                if (candidate.TargetId != null &&
                    (!context.Memory.TryGetValue(candidate.TargetId, out var fact) ||
                     fact.IsExpired(context.CurrentTimeMs) ||
                     !fact.Provenance.Contains("vision", StringComparison.OrdinalIgnoreCase)))
                {
                    // Descartado: Intento de persecución sin visión activa
                    continue;
                }
            }

            // Regla 6: Seguir (Follow) requiere una posición objetivo válida
            if (candidate.Kind == ActionKind.Follow && !candidate.TargetPosition.HasValue)
            {
                continue;
            }

            result.Add(candidate);
        }

        return result;
    }
}
