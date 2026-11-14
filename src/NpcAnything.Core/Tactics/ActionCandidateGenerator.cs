using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Tactics;

/// <summary>
/// Generador de candidatos de acción puramente táctico desacoplado de cualquier motor.
/// Evalúa el contexto del NPC (memoria, rol, salud y coberturas) y produce los candidatos
/// que competirán en el selector de utilidad.
/// </summary>
public class ActionCandidateGenerator
{
    public float MaxAttackRange { get; set; } = 12.0f;
    public float MinVisualConfidenceToAttack { get; set; } = 0.70f;

    /// <summary>
    /// Genera la lista de candidatos de acción a partir del estado contextual permitido.
    /// Garantiza que la memoria auditiva o desactualizada solo habilite 'Investigate' o 'TakeCover',
    /// nunca 'Attack' directo a ciegas.
    /// </summary>
    public IReadOnlyList<ActionCandidate> GenerateCandidates(NpcContext context)
    {
        var candidates = new List<ActionCandidate>();

        // Buscar el objetivo principal en memoria no expirada
        var targetFact = context.Memory.Values.FirstOrDefault(f => !f.IsExpired(context.CurrentTimeMs));

        if (targetFact != null)
        {
            float dist = context.Position.DistanceTo(targetFact.EstimatedPosition);
            float confidence = targetFact.GetDecayedConfidence(context.CurrentTimeMs);
            bool isVisualContact = targetFact.Provenance.Contains("vision", StringComparison.OrdinalIgnoreCase);

            // 1. Candidato de Ataque:
            // EXCLUSIVO para contacto visual fresco con alta certeza y dentro de rango.
            if (isVisualContact && confidence >= MinVisualConfidenceToAttack && dist <= MaxAttackRange)
            {
                float proximityBonus = 1.0f - (dist / MaxAttackRange);
                float attackScore = (proximityBonus * 0.6f) + (confidence * 0.4f);

                candidates.Add(new ActionCandidate(
                    ActionKind.Attack,
                    attackScore,
                    "TARGET_IN_SIGHT_AND_RANGE",
                    TargetId: targetFact.SubjectId,
                    TargetPosition: targetFact.EstimatedPosition));
            }

            // 2. Candidato de Investigación:
            // Para contactos auditivos o cuando se pierde contacto visual directo (búsqueda del último avistamiento).
            if (!isVisualContact || confidence < MinVisualConfidenceToAttack)
            {
                float searchScore = Math.Clamp(confidence * 0.75f, 0.40f, 0.85f);
                string reason = isVisualContact ? "CONTACT_LOST_SEARCH_LAST_SEEN" : "ACOUSTIC_LEAD_INVESTIGATE_SOUND";

                candidates.Add(new ActionCandidate(
                    ActionKind.Investigate,
                    searchScore,
                    reason,
                    TargetPosition: targetFact.EstimatedPosition));
            }

            // 3. Candidato de Cobertura:
            // Si hay amenaza activa o la salud está comprometida y existen coberturas transitables.
            if (context.ReachableCoverPoints.Count > 0)
            {
                Vec3 bestCover = context.ReachableCoverPoints[0];
                float threatUrgency = (1.0f - context.HealthRatio) * 0.6f + 0.35f;

                // Rol de flanqueador o soporte pondera más la cobertura
                if (context.Role is NpcRole.Flanker or NpcRole.Support)
                {
                    threatUrgency += 0.15f;
                }

                candidates.Add(new ActionCandidate(
                    ActionKind.TakeCover,
                    Math.Clamp(threatUrgency, 0.2f, 0.95f),
                    "ACTIVE_THREAT_SEEK_COVER",
                    TargetPosition: bestCover));
            }
        }
        else
        {
            // Sin información activa en memoria: patrullar o mantener posición
            candidates.Add(new ActionCandidate(
                ActionKind.Hold,
                0.40f,
                "IDLE_STANDBY"));

            candidates.Add(new ActionCandidate(
                ActionKind.Patrol,
                0.50f,
                "SEARCH_PATROL"));
        }

        return candidates;
    }
}
