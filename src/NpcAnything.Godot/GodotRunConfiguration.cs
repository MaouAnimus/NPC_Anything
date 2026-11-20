using NpcAnything.Core.Configuration;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Reproducible run metadata shared by the demo and Godot adapter.
/// </summary>
public static class GodotRunConfiguration
{
    public static string ScenarioId { get; private set; } = "SC02_CORE";
    public static int Seed { get; private set; } = 42;
    public static DifficultyLevel Difficulty { get; private set; } = DifficultyLevel.Normal;
    public static DifficultyProfile ActiveDifficultyProfile => DifficultyProfile.ForLevel(Difficulty);

    public static void Configure(string scenarioId, int seed, DifficultyLevel difficulty = DifficultyLevel.Normal)
    {
        ScenarioId = scenarioId;
        Seed = seed;
        Difficulty = difficulty;
    }
}
