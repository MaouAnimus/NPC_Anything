using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Utility;

/// <summary>
/// Selector de decisiones basado en Utility AI con histéresis y fallback seguro.
/// Totalmente determinista y puro.
/// </summary>
public class UtilitySelector
{
    private readonly float _hysteresisMargin;
    private readonly float _minimumViableScore;

    public UtilitySelector(float hysteresisMargin = 0.10f, float minimumViableScore = 0.15f)
    {
        _hysteresisMargin = Math.Clamp(hysteresisMargin, 0f, 0.5f);
        _minimumViableScore = Math.Clamp(minimumViableScore, 0f, 1f);
    }

    /// <summary>
    /// Selecciona la mejor acción a partir de una lista de candidatos evaluados.
    /// </summary>
    public ActionDecision SelectBestAction(IReadOnlyList<ActionCandidate> candidates, NpcContext context)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return ActionDecision.FallbackHold("FALLBACK_NO_CANDIDATES", context.CurrentTimeMs);
        }

        // Filtrar candidatos con score viable
        var validCandidates = candidates.Where(c => c.Score >= _minimumViableScore).ToList();
        if (validCandidates.Count == 0)
        {
            return ActionDecision.FallbackHold("FALLBACK_ALL_SCORES_BELOW_THRESHOLD", context.CurrentTimeMs);
        }

        // Ordenar por score descendente
        var sorted = validCandidates.OrderByDescending(c => c.Score).ToList();
        var topCandidate = sorted[0];

        // Aplicar histéresis si la acción elegida difiere de la actual
        if (topCandidate.Kind != context.CurrentAction)
        {
            var currentCandidate = validCandidates.FirstOrDefault(c => c.Kind == context.CurrentAction);
            if (currentCandidate != null)
            {
                // Solo cambiar si el nuevo candidato supera a la acción actual por encima del margen de histéresis
                if (topCandidate.Score < (currentCandidate.Score + _hysteresisMargin))
                {
                    return new ActionDecision(
                        currentCandidate.Kind,
                        currentCandidate.Score,
                        $"{currentCandidate.ReasonCode}_RETAINED_BY_HYSTERESIS",
                        currentCandidate.TargetId,
                        currentCandidate.TargetPosition,
                        context.CurrentTimeMs);
                }
            }
        }

        return new ActionDecision(
            topCandidate.Kind,
            topCandidate.Score,
            topCandidate.ReasonCode,
            topCandidate.TargetId,
            topCandidate.TargetPosition,
            context.CurrentTimeMs);
    }
}
