using System;
using System.Text.Json;

namespace NpcAnything.Core.Configuration;

/// <summary>
/// Parser y validador de configuración de perfiles de dificultad (RF-18, RNF-08).
/// Falla de manera segura con mensajes claros ante datos corruptos o intentos de violación de equidad.
/// </summary>
public static class DifficultyProfileParser
{
    /// <summary>
    /// Intenta parsear el nombre de un nivel de dificultad a partir de una cadena (inglés o español).
    /// </summary>
    public static bool TryParseLevel(string? text, out DifficultyLevel level)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            level = DifficultyLevel.Normal;
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "easy":
            case "facil":
            case "fácil":
                level = DifficultyLevel.Easy;
                return true;

            case "normal":
            case "medio":
                level = DifficultyLevel.Normal;
                return true;

            case "hard":
            case "dificil":
            case "difícil":
                level = DifficultyLevel.Hard;
                return true;

            case "expert":
            case "experto":
                level = DifficultyLevel.Expert;
                return true;

            default:
                level = DifficultyLevel.Normal;
                return false;
        }
    }

    /// <summary>
    /// Parsea una representación JSON de perfil de dificultad o retorna un fallback seguro (RNF-08).
    /// Si detecta multiplicadores de HP o daño alterados (violación de RF-11), emite advertencia y los ignora.
    /// </summary>
    public static DifficultyProfile ParseOrFallback(
        string? jsonContent,
        DifficultyLevel defaultLevel = DifficultyLevel.Normal,
        Action<string>? warningLogger = null)
    {
        DifficultyProfile fallback = DifficultyProfile.ForLevel(defaultLevel);

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return fallback;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Nivel de dificultad
            DifficultyLevel level = defaultLevel;
            if (root.TryGetProperty("level", out var levelElem) && levelElem.ValueKind == JsonValueKind.String)
            {
                if (!TryParseLevel(levelElem.GetString(), out level))
                {
                    warningLogger?.Invoke($"[DifficultyProfileParser] Nivel desconocido '{levelElem.GetString()}'; usando fallback {defaultLevel}.");
                    level = defaultLevel;
                }
            }

            // Detección estricta de violaciones de juego limpio (RF-11)
            if (root.TryGetProperty("hp_multiplier", out var hpElem) && hpElem.ValueKind == JsonValueKind.Number)
            {
                float hpVal = (float)hpElem.GetDouble();
                if (Math.Abs(hpVal - 1.0f) > 0.001f)
                {
                    warningLogger?.Invoke($"[DifficultyProfileParser] VIOLACIÓN RF-11: hp_multiplier ({hpVal}) debe ser estrictamente 1.0. Forzado a 1.0f.");
                }
            }

            if (root.TryGetProperty("damage_multiplier", out var dmgElem) && dmgElem.ValueKind == JsonValueKind.Number)
            {
                float dmgVal = (float)dmgElem.GetDouble();
                if (Math.Abs(dmgVal - 1.0f) > 0.001f)
                {
                    warningLogger?.Invoke($"[DifficultyProfileParser] VIOLACIÓN RF-11: damage_multiplier ({dmgVal}) debe ser estrictamente 1.0. Forzado a 1.0f.");
                }
            }

            // Parámetros cognitivos y de coordinación con defaults seguros
            float reactionDelay = fallback.ReactionDelaySeconds;
            if (root.TryGetProperty("reaction_delay_s", out var rdElem) && rdElem.ValueKind == JsonValueKind.Number)
            {
                float val = (float)rdElem.GetDouble();
                if (val > 0f) reactionDelay = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] reaction_delay_s debe ser positivo.");
            }
            else if (root.TryGetProperty("reaction_delay_ms", out var rdMsElem) && rdMsElem.ValueKind == JsonValueKind.Number)
            {
                float val = (float)rdMsElem.GetDouble() / 1000f;
                if (val > 0f) reactionDelay = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] reaction_delay_ms debe ser positivo.");
            }

            float soundLocationError = fallback.SoundLocationErrorMeters;
            if (root.TryGetProperty("sound_location_error_m", out var sleElem) && sleElem.ValueKind == JsonValueKind.Number)
            {
                float val = (float)sleElem.GetDouble();
                if (val >= 0f) soundLocationError = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] sound_location_error_m no puede ser negativo.");
            }

            long memoryTtlMs = fallback.MemoryTtlMs;
            if (root.TryGetProperty("search_memory_ttl_ms", out var memElem) && memElem.ValueKind == JsonValueKind.Number)
            {
                long val = memElem.GetInt64();
                if (val > 0) memoryTtlMs = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] search_memory_ttl_ms debe ser positivo.");
            }

            long squadMessageDelayMs = fallback.SquadMessageDelayMs;
            if (root.TryGetProperty("squad_message_delay_ms", out var smdElem) && smdElem.ValueKind == JsonValueKind.Number)
            {
                long val = smdElem.GetInt64();
                if (val >= 0) squadMessageDelayMs = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] squad_message_delay_ms no puede ser negativo.");
            }

            float coordinationWeight = fallback.CoordinationWeight;
            if (root.TryGetProperty("coordination_weight", out var cwElem) && cwElem.ValueKind == JsonValueKind.Number)
            {
                float val = (float)cwElem.GetDouble();
                if (val >= 0f && val <= 1.0f) coordinationWeight = val;
                else warningLogger?.Invoke("[DifficultyProfileParser] coordination_weight debe estar entre 0.0 y 1.0.");
            }

            return new DifficultyProfile(
                level,
                reactionDelay,
                soundLocationError,
                memoryTtlMs,
                squadMessageDelayMs,
                coordinationWeight);
        }
        catch (Exception ex)
        {
            warningLogger?.Invoke($"[DifficultyProfileParser] Error parseando perfil JSON: {ex.Message}. Utilizando perfil de fallback.");
            return fallback;
        }
    }
}
