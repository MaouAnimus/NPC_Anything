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
    public float MaxVisionDistance { get; set; } = 20.0f;
    public float MinVisualConfidenceToAttack { get; set; } = 0.70f;
    public RoleTacticalProfile? CustomRoleProfile { get; set; } = null;

    /// <summary>
    /// Genera la lista de candidatos de acción a partir del estado contextual permitido y el perfil de rol.
    /// Garantiza que la memoria auditiva o desactualizada solo habilite 'Investigate' o 'TakeCover',
    /// nunca 'Attack' directo a ciegas.
    /// </summary>
    public IReadOnlyList<ActionCandidate> GenerateCandidates(NpcContext context)
    {
        var candidates = new List<ActionCandidate>();
        var profile = CustomRoleProfile?.Role == context.Role ? CustomRoleProfile : RoleTacticalProfile.ForRole(context.Role);

        // Buscar el objetivo principal en memoria no expirada
        var targetFact = context.Memory.Values.FirstOrDefault(f => !f.IsExpired(context.CurrentTimeMs));

        if (targetFact != null)
        {
            float dist = context.Position.DistanceTo(targetFact.EstimatedPosition);
            float confidence = targetFact.GetDecayedConfidence(context.CurrentTimeMs);
            bool isVisualContact = targetFact.Provenance.Contains("vision", StringComparison.OrdinalIgnoreCase);

            // 1. Candidato de Ataque:
            // EXCLUSIVO para contacto visual fresco con alta certeza y dentro de rango de disparo (<= 12m).
            if (isVisualContact && confidence >= MinVisualConfidenceToAttack && dist <= MaxAttackRange)
            {
                float proximityBonus = 1.0f - (dist / MaxAttackRange);
                float rawScore = (proximityBonus * 0.6f) + (confidence * 0.4f);
                float attackScore = Math.Clamp(rawScore * profile.AttackWeight, 0.10f, 1.0f);

                candidates.Add(new ActionCandidate(
                    ActionKind.Attack,
                    attackScore,
                    "TARGET_IN_SIGHT_AND_RANGE",
                    TargetId: targetFact.SubjectId,
                    TargetPosition: targetFact.EstimatedPosition));
            }

            // 2. Candidato de Persecución (Pursue):
            // Contacto visual fresco directo fuera de alcance de ataque pero dentro de alcance visual (12m a 20m).
            // Permite al NPC aproximarse activamente hacia el objetivo visible según la agresividad del rol.
            if (isVisualContact && confidence >= MinVisualConfidenceToAttack && dist > MaxAttackRange && dist <= MaxVisionDistance)
            {
                float rawPursue = (confidence * 0.5f) + 0.35f;
                float pursueScore = Math.Clamp(rawPursue * profile.PursueWeight, 0.20f, 0.95f);

                // Si es Companion y está a más de 8m de su aliado protegido, frena la persecución para no abandonarlo
                if (context.Role == NpcRole.Companion && context.KnownAllyPositions.Count > 0)
                {
                    float distToAlly = context.Position.DistanceTo(context.KnownAllyPositions[0]);
                    if (distToAlly > 8.0f)
                    {
                        pursueScore *= 0.50f;
                    }
                }

                candidates.Add(new ActionCandidate(
                    ActionKind.Pursue,
                    pursueScore,
                    "TARGET_IN_SIGHT_CLOSE_DISTANCE",
                    TargetId: targetFact.SubjectId,
                    TargetPosition: targetFact.EstimatedPosition));
            }

            // 3. Candidato de Investigación:
            // Para contactos auditivos, pérdida de certeza visual o cuando el objetivo excede el rango de visión (>20m)
            // (investigación de la última posición conocida sin ataque ni persecución directa).
            if (!isVisualContact || confidence < MinVisualConfidenceToAttack || dist > MaxVisionDistance)
            {
                float rawSearch = confidence * 0.75f;
                float searchScore = Math.Clamp(rawSearch * profile.InvestigateWeight, 0.30f, 0.95f);
                string reason = (!isVisualContact) ? "ACOUSTIC_LEAD_INVESTIGATE_SOUND" : "CONTACT_LOST_SEARCH_LAST_SEEN";

                candidates.Add(new ActionCandidate(
                    ActionKind.Investigate,
                    searchScore,
                    reason,
                    TargetPosition: targetFact.EstimatedPosition));
            }

            // 4. Candidato de Cobertura (TakeCover):
            // Si hay amenaza activa o la salud está comprometida y existen coberturas transitables.
            if (context.ReachableCoverPoints.Count > 0)
            {
                Vec3 bestCover = context.ReachableCoverPoints[0];

                // El Companion prefiere una cobertura cercana a su aliado protegido para defenderlo
                if (context.Role == NpcRole.Companion && context.KnownAllyPositions.Count > 0)
                {
                    Vec3 allyPos = context.KnownAllyPositions[0];
                    float minAllyDist = float.MaxValue;
                    foreach (var c in context.ReachableCoverPoints)
                    {
                        float d = c.DistanceTo(allyPos);
                        if (d < minAllyDist)
                        {
                            minAllyDist = d;
                            bestCover = c;
                        }
                    }
                }

                float healthDeficit = 1.0f - context.HealthRatio;
                float threatUrgency = (healthDeficit >= (1.0f - profile.CriticalHealthThreshold))
                    ? (healthDeficit * 0.7f + 0.35f)
                    : (healthDeficit * 0.4f + 0.20f);

                // El Scout es cauteloso: ante presencia de amenaza incrementa su urgencia defensiva
                if (context.Role == NpcRole.Scout)
                {
                    threatUrgency += 0.15f;
                }

                float coverScore = Math.Clamp(threatUrgency * profile.CoverWeight, 0.2f, 0.95f);

                candidates.Add(new ActionCandidate(
                    ActionKind.TakeCover,
                    coverScore,
                    "ACTIVE_THREAT_SEEK_COVER",
                    TargetPosition: bestCover));
            }

            // 5. Candidato de Flanqueo (Flank):
            // Evalúa si existen coberturas transitables que ofrezcan un ángulo lateral respecto al objetivo.
            if (context.ReachableCoverPoints.Count > 0)
            {
                Vec3? bestFlankCover = null;
                float bestFlankAngle = 0f;

                Vec3 toTarget = targetFact.EstimatedPosition - context.Position;
                float toTargetLen = (float)Math.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);

                if (toTargetLen > 0.01f)
                {
                    foreach (var cover in context.ReachableCoverPoints)
                    {
                        Vec3 toCover = cover - context.Position;
                        float toCoverLen = (float)Math.Sqrt(toCover.X * toCover.X + toCover.Z * toCover.Z);
                        if (toCoverLen < 0.01f) continue;

                        float dot = (toTarget.X * toCover.X + toTarget.Z * toCover.Z) / (toTargetLen * toCoverLen);
                        dot = Math.Clamp(dot, -1f, 1f);
                        float angleDeg = (float)(Math.Acos(dot) * (180.0 / Math.PI));

                        // Trayectoria angular lateral entre 30° y 120°
                        if (angleDeg >= 30f && angleDeg <= 120f)
                        {
                            if (angleDeg > bestFlankAngle)
                            {
                                bestFlankAngle = angleDeg;
                                bestFlankCover = cover;
                            }
                        }
                    }
                }

                if (bestFlankCover.HasValue)
                {
                    float flankScore = Math.Clamp((0.50f + (bestFlankAngle / 180f) * 0.35f) * profile.FlankWeight, 0.25f, 0.95f);
                    candidates.Add(new ActionCandidate(
                        ActionKind.Flank,
                        flankScore,
                        "FLANK_LATERAL_ROUTE",
                        TargetId: targetFact.SubjectId,
                        TargetPosition: bestFlankCover.Value));
                }
            }
        }
        else
        {
            // Sin información activa en memoria: patrullar o mantener posición
            Vec3? patrolTarget = null;
            if (context.PatrolWaypoints.Count > 0)
            {
                // Seleccionar el waypoint adecuado según proximidad
                int targetIndex = 0;
                float nearestDistSq = float.MaxValue;
                for (int i = 0; i < context.PatrolWaypoints.Count; i++)
                {
                    float dSq = context.Position.DistanceSquaredTo(context.PatrolWaypoints[i]);
                    if (dSq < nearestDistSq)
                    {
                        nearestDistSq = dSq;
                        targetIndex = i;
                    }
                }

                // Si ya estamos prácticamente en el waypoint más cercano (umbral 1.5m), avanzar al siguiente en la secuencia
                if (nearestDistSq <= 2.25f)
                {
                    targetIndex = (targetIndex + 1) % context.PatrolWaypoints.Count;
                }

                patrolTarget = context.PatrolWaypoints[targetIndex];
            }

            candidates.Add(new ActionCandidate(
                ActionKind.Hold,
                0.30f,
                "IDLE_STANDBY"));

            // Con score de 0.60f, Patrol supera a Hold (0.30f + histéresis 0.12f = 0.42f)
            // garantizando la transición desde el estado inicial y manteniendo patrulla activa con destino explícito.
            float patrolScore = patrolTarget.HasValue ? 0.60f : 0.45f;
            candidates.Add(new ActionCandidate(
                ActionKind.Patrol,
                patrolScore,
                patrolTarget.HasValue ? "WAYPOINT_PATROL" : "SEARCH_PATROL",
                TargetPosition: patrolTarget));
        }

        return candidates;
    }
}
