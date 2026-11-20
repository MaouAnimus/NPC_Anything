using System;

namespace NpcAnything.Core.Configuration;

/// <summary>
/// Nivel de dificultad táctica del NPC (RF-11, RF-18, CU-06).
/// Modula las tolerancias perceptivas, latencias de reacción y coordinación
/// sin modificar jamás la salud (HP) ni el daño base de los combatientes.
/// </summary>
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard,
    Expert
}

/// <summary>
/// Perfil desacoplado de dificultad que gobierna los parámetros de cognición y coordinación (BK-023).
/// INVARIANTE INQUEBRANTABLE (RF-11, Política de Experiencia 4):
/// FixedHpMultiplier = 1.0f y FixedDamageMultiplier = 1.0f para todos los perfiles.
/// </summary>
public record DifficultyProfile
{
    public DifficultyLevel Level { get; init; }
    public float ReactionDelaySeconds { get; init; }
    public float SoundLocationErrorMeters { get; init; }
    public long MemoryTtlMs { get; init; }
    public long SquadMessageDelayMs { get; init; }
    public float CoordinationWeight { get; init; }

    /// <summary>
    /// Multiplicador de salud fijo garantizado por contrato (RF-11).
    /// </summary>
    public float FixedHpMultiplier => 1.0f;

    /// <summary>
    /// Multiplicador de daño infligido fijo garantizado por contrato (RF-11).
    /// </summary>
    public float FixedDamageMultiplier => 1.0f;

    public DifficultyProfile(
        DifficultyLevel level,
        float reactionDelaySeconds,
        float soundLocationErrorMeters,
        long memoryTtlMs,
        long squadMessageDelayMs,
        float coordinationWeight)
    {
        if (reactionDelaySeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(reactionDelaySeconds), "Reaction delay must be strictly positive.");
        if (soundLocationErrorMeters < 0f)
            throw new ArgumentOutOfRangeException(nameof(soundLocationErrorMeters), "Sound location error cannot be negative.");
        if (memoryTtlMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(memoryTtlMs), "Memory TTL must be strictly positive.");
        if (squadMessageDelayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(squadMessageDelayMs), "Squad message delay cannot be negative.");
        if (coordinationWeight < 0f || coordinationWeight > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(coordinationWeight), "Coordination weight must be between 0.0 and 1.0.");

        Level = level;
        ReactionDelaySeconds = reactionDelaySeconds;
        SoundLocationErrorMeters = soundLocationErrorMeters;
        MemoryTtlMs = memoryTtlMs;
        SquadMessageDelayMs = squadMessageDelayMs;
        CoordinationWeight = coordinationWeight;
    }

    /// <summary>
    /// Retorna los parámetros de dificultad calibrados por defecto (DoD BK-023, examples/difficulty_profiles.yaml).
    /// </summary>
    public static DifficultyProfile ForLevel(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Easy => new(
            DifficultyLevel.Easy,
            reactionDelaySeconds: 0.85f,
            soundLocationErrorMeters: 6.0f,
            memoryTtlMs: 7000,
            squadMessageDelayMs: 1500,
            coordinationWeight: 0.25f),

        DifficultyLevel.Normal => new(
            DifficultyLevel.Normal,
            reactionDelaySeconds: 0.55f,
            soundLocationErrorMeters: 4.0f,
            memoryTtlMs: 11000,
            squadMessageDelayMs: 900,
            coordinationWeight: 0.50f),

        DifficultyLevel.Hard => new(
            DifficultyLevel.Hard,
            reactionDelaySeconds: 0.35f,
            soundLocationErrorMeters: 2.5f,
            memoryTtlMs: 16000,
            squadMessageDelayMs: 500,
            coordinationWeight: 0.70f),

        DifficultyLevel.Expert => new(
            DifficultyLevel.Expert,
            reactionDelaySeconds: 0.26f,
            soundLocationErrorMeters: 1.7f,
            memoryTtlMs: 19000,
            squadMessageDelayMs: 350,
            coordinationWeight: 0.85f),

        _ => ForLevel(DifficultyLevel.Normal)
    };

    public static DifficultyProfile Default => ForLevel(DifficultyLevel.Normal);
}
