using System;
using System.Collections.Generic;
using NpcAnything.Core.Configuration;
using Xunit;

namespace Core.Tests;

/// <summary>
/// Pruebas unitarias para el sistema de perfiles de dificultad desacoplado (BK-023, RF-11, RF-18, RNF-08).
/// </summary>
public class DifficultyProfileTests
{
    [Fact]
    public void FairnessInvariant_HpAndDamageMultipliersAreStrictlyOneAcrossAllLevels()
    {
        // REGLA FUNDAMENTAL RF-11: La dificultad nunca debe alterar el HP ni el daño base.
        foreach (DifficultyLevel level in Enum.GetValues<DifficultyLevel>())
        {
            var profile = DifficultyProfile.ForLevel(level);

            Assert.Equal(1.0f, profile.FixedHpMultiplier);
            Assert.Equal(1.0f, profile.FixedDamageMultiplier);
        }
    }

    [Fact]
    public void Monotonicity_CognitiveAndCoordinationParametersScaleStrictlyWithDifficulty()
    {
        var easy = DifficultyProfile.ForLevel(DifficultyLevel.Easy);
        var normal = DifficultyProfile.ForLevel(DifficultyLevel.Normal);
        var hard = DifficultyProfile.ForLevel(DifficultyLevel.Hard);
        var expert = DifficultyProfile.ForLevel(DifficultyLevel.Expert);

        // Latencia de reacción: disminuye estrictamente a mayor dificultad
        Assert.True(easy.ReactionDelaySeconds > normal.ReactionDelaySeconds, "Easy reaction delay should be slower than Normal.");
        Assert.True(normal.ReactionDelaySeconds > hard.ReactionDelaySeconds, "Normal reaction delay should be slower than Hard.");
        Assert.True(hard.ReactionDelaySeconds > expert.ReactionDelaySeconds, "Hard reaction delay should be slower than Expert.");

        // Incertidumbre acústica: disminuye estrictamente a mayor dificultad (mejor localización)
        Assert.True(easy.SoundLocationErrorMeters > normal.SoundLocationErrorMeters, "Easy sound error should be larger than Normal.");
        Assert.True(normal.SoundLocationErrorMeters > hard.SoundLocationErrorMeters, "Normal sound error should be larger than Hard.");
        Assert.True(hard.SoundLocationErrorMeters > expert.SoundLocationErrorMeters, "Hard sound error should be larger than Expert.");

        // Persistencia en memoria (TTL): se incrementa a mayor dificultad (mayor memoria táctica)
        Assert.True(easy.MemoryTtlMs < normal.MemoryTtlMs, "Easy memory TTL should be shorter than Normal.");
        Assert.True(normal.MemoryTtlMs < hard.MemoryTtlMs, "Normal memory TTL should be shorter than Hard.");
        Assert.True(hard.MemoryTtlMs < expert.MemoryTtlMs, "Hard memory TTL should be shorter than Expert.");

        // Retardo de radio de escuadrón: disminuye a mayor dificultad (comunicación más ágil)
        Assert.True(easy.SquadMessageDelayMs > normal.SquadMessageDelayMs, "Easy squad message delay should be longer than Normal.");
        Assert.True(normal.SquadMessageDelayMs > hard.SquadMessageDelayMs, "Normal squad message delay should be longer than Hard.");
        Assert.True(hard.SquadMessageDelayMs > expert.SquadMessageDelayMs, "Hard squad message delay should be longer than Expert.");

        // Peso de coordinación táctica: se incrementa a mayor dificultad
        Assert.True(easy.CoordinationWeight < normal.CoordinationWeight, "Easy coordination weight should be lower than Normal.");
        Assert.True(normal.CoordinationWeight < hard.CoordinationWeight, "Normal coordination weight should be lower than Hard.");
        Assert.True(hard.CoordinationWeight < expert.CoordinationWeight, "Hard coordination weight should be lower than Expert.");
    }

    [Theory]
    [InlineData(0f, 4.0f, 1000, 500, 0.5f)]    // reaction delay 0
    [InlineData(-0.1f, 4.0f, 1000, 500, 0.5f)] // negative reaction delay
    [InlineData(0.5f, -1.0f, 1000, 500, 0.5f)] // negative sound error
    [InlineData(0.5f, 4.0f, 0, 500, 0.5f)]      // zero memory ttl
    [InlineData(0.5f, 4.0f, -100, 500, 0.5f)]   // negative memory ttl
    [InlineData(0.5f, 4.0f, 1000, -1, 0.5f)]    // negative squad delay
    [InlineData(0.5f, 4.0f, 1000, 500, -0.1f)]  // coordination weight < 0
    [InlineData(0.5f, 4.0f, 1000, 500, 1.1f)]   // coordination weight > 1
    public void ConstructorValidation_RejectsInvalidRanges(
        float reactionDelay,
        float soundError,
        long memoryTtl,
        long squadDelay,
        float coordWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DifficultyProfile(
            DifficultyLevel.Normal,
            reactionDelay,
            soundError,
            memoryTtl,
            squadDelay,
            coordWeight));
    }

    [Theory]
    [InlineData("easy", DifficultyLevel.Easy)]
    [InlineData("EASY", DifficultyLevel.Easy)]
    [InlineData("facil", DifficultyLevel.Easy)]
    [InlineData("fácil", DifficultyLevel.Easy)]
    [InlineData("normal", DifficultyLevel.Normal)]
    [InlineData("Normal", DifficultyLevel.Normal)]
    [InlineData("medio", DifficultyLevel.Normal)]
    [InlineData("hard", DifficultyLevel.Hard)]
    [InlineData("HARD", DifficultyLevel.Hard)]
    [InlineData("dificil", DifficultyLevel.Hard)]
    [InlineData("difícil", DifficultyLevel.Hard)]
    [InlineData("expert", DifficultyLevel.Expert)]
    [InlineData("EXPERT", DifficultyLevel.Expert)]
    [InlineData("experto", DifficultyLevel.Expert)]
    public void Parser_TryParseLevel_RecognizesValidNames(string input, DifficultyLevel expected)
    {
        bool success = DifficultyProfileParser.TryParseLevel(input, out var level);
        Assert.True(success);
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nightmare")]
    [InlineData("godmode")]
    public void Parser_TryParseLevel_RejectsUnknownValues(string input)
    {
        bool success = DifficultyProfileParser.TryParseLevel(input, out var level);
        Assert.False(success);
        Assert.Equal(DifficultyLevel.Normal, level);
    }

    [Fact]
    public void Parser_ParseOrFallback_ValidJson_ReturnsCustomProfile()
    {
        string json = """
        {
            "level": "hard",
            "reaction_delay_ms": 320,
            "sound_location_error_m": 2.2,
            "search_memory_ttl_ms": 15000,
            "squad_message_delay_ms": 400,
            "coordination_weight": 0.75
        }
        """;

        var profile = DifficultyProfileParser.ParseOrFallback(json);

        Assert.Equal(DifficultyLevel.Hard, profile.Level);
        Assert.Equal(0.320f, profile.ReactionDelaySeconds, precision: 3);
        Assert.Equal(2.2f, profile.SoundLocationErrorMeters, precision: 1);
        Assert.Equal(15000, profile.MemoryTtlMs);
        Assert.Equal(400, profile.SquadMessageDelayMs);
        Assert.Equal(0.75f, profile.CoordinationWeight, precision: 2);
        Assert.Equal(1.0f, profile.FixedHpMultiplier);
        Assert.Equal(1.0f, profile.FixedDamageMultiplier);
    }

    [Fact]
    public void Parser_ParseOrFallback_CheatingHpAndDamage_EnforcesFairnessAndLogsWarning()
    {
        // Intento de hacer trampa modificando HP o daño (Violación de RF-11)
        string cheatJson = """
        {
            "level": "expert",
            "hp_multiplier": 5.0,
            "damage_multiplier": 10.0,
            "reaction_delay_ms": 250
        }
        """;

        var warnings = new List<string>();
        var profile = DifficultyProfileParser.ParseOrFallback(cheatJson, warningLogger: warnings.Add);

        // Se deben registrar las advertencias de violación
        Assert.Contains(warnings, w => w.Contains("VIOLACIÓN RF-11") && w.Contains("hp_multiplier"));
        Assert.Contains(warnings, w => w.Contains("VIOLACIÓN RF-11") && w.Contains("damage_multiplier"));

        // Los multiplicadores de combate permanecen invariantes en 1.0f
        Assert.Equal(1.0f, profile.FixedHpMultiplier);
        Assert.Equal(1.0f, profile.FixedDamageMultiplier);
        Assert.Equal(DifficultyLevel.Expert, profile.Level);
        Assert.Equal(0.25f, profile.ReactionDelaySeconds, precision: 2);
    }

    [Fact]
    public void Parser_ParseOrFallback_MalformedJson_SafelyRecoversWithFallback()
    {
        string corruptJson = "{ invalid_json_syntax: true,,, ";
        var warnings = new List<string>();

        var profile = DifficultyProfileParser.ParseOrFallback(
            corruptJson,
            defaultLevel: DifficultyLevel.Easy,
            warningLogger: warnings.Add);

        Assert.Single(warnings);
        Assert.Contains("Error parseando perfil JSON", warnings[0]);
        Assert.Equal(DifficultyLevel.Easy, profile.Level);
        Assert.Equal(0.85f, profile.ReactionDelaySeconds);
    }
}
