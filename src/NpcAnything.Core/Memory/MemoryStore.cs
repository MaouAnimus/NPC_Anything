using System.Collections.Concurrent;
using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Memory;

/// <summary>
/// Gestiona la memoria sensorial a corto plazo de un NPC con caducidad (TTL) y degradación de certeza.
/// </summary>
public class MemoryStore
{
    private readonly ConcurrentDictionary<string, MemoryFact> _facts = new();
    private readonly long _defaultTtlMs;

    public MemoryStore(long defaultTtlMs = 4000)
    {
        _defaultTtlMs = defaultTtlMs;
    }

    /// <summary>
    /// Incorpora nuevos estímulos sensoriales actualizando la estimación de posición y confianza.
    /// </summary>
    public void ProcessStimuli(IEnumerable<Stimulus> stimuli, long currentTimeMs)
    {
        foreach (var stimulus in stimuli)
        {
            long ttl = stimulus.Kind switch
            {
                StimulusType.Visual => _defaultTtlMs,
                StimulusType.Audio => _defaultTtlMs / 2,
                StimulusType.Damage => _defaultTtlMs * 2,
                StimulusType.SquadMessage => _defaultTtlMs,
                _ => _defaultTtlMs
            };

            _facts.AddOrUpdate(
                stimulus.SubjectId,
                _ => new MemoryFact(
                    stimulus.SubjectId,
                    stimulus.EstimatedPosition,
                    stimulus.Confidence,
                    stimulus.PrecisionMeters,
                    currentTimeMs,
                    currentTimeMs + ttl,
                    stimulus.SourceId),
                (_, existing) =>
                {
                    // La visión directa prevalece sobre señales previas o auditivas
                    if (stimulus.Kind == StimulusType.Visual || existing.IsExpired(currentTimeMs))
                    {
                        return new MemoryFact(
                            stimulus.SubjectId,
                            stimulus.EstimatedPosition,
                            stimulus.Confidence,
                            stimulus.PrecisionMeters,
                            currentTimeMs,
                            currentTimeMs + ttl,
                            stimulus.SourceId);
                    }

                    // Fusión ponderada si el nuevo estímulo es auditivo y aún existe memoria visual fresca
                    float decayedConf = existing.GetDecayedConfidence(currentTimeMs);
                    if (stimulus.Confidence >= decayedConf)
                    {
                        return new MemoryFact(
                            stimulus.SubjectId,
                            stimulus.EstimatedPosition,
                            stimulus.Confidence,
                            stimulus.PrecisionMeters,
                            currentTimeMs,
                            currentTimeMs + ttl,
                            stimulus.SourceId);
                    }

                    // Se conserva la existente pero se renueva ligeramente el TTL por actividad
                    return existing with { ExpiresAtMs = Math.Max(existing.ExpiresAtMs, currentTimeMs + (ttl / 2)) };
                });
        }
    }

    /// <summary>
    /// Consulta el estado actual de un hecho en memoria para un sujeto.
    /// Retorna null si no existe o ya expiró su TTL.
    /// </summary>
    public MemoryFact? GetFact(string subjectId, long currentTimeMs)
    {
        if (_facts.TryGetValue(subjectId, out var fact))
        {
            if (fact.IsExpired(currentTimeMs))
            {
                _facts.TryRemove(subjectId, out _);
                return null;
            }
            return fact;
        }
        return null;
    }

    /// <summary>
    /// Retorna una instantánea inmutable de todos los hechos activos no expirados.
    /// </summary>
    public IReadOnlyDictionary<string, MemoryFact> GetActiveFacts(long currentTimeMs)
    {
        PurgeExpired(currentTimeMs);
        return _facts.ToDictionary(k => k.Key, v => v.Value);
    }

    /// <summary>
    /// Elimina de forma determinista todos los hechos que hayan rebasado su tiempo de expiración.
    /// </summary>
    public int PurgeExpired(long currentTimeMs)
    {
        int purged = 0;
        foreach (var (key, fact) in _facts)
        {
            if (fact.IsExpired(currentTimeMs))
            {
                if (_facts.TryRemove(key, out _))
                {
                    purged++;
                }
            }
        }
        return purged;
    }

    public void Clear() => _facts.Clear();
}
