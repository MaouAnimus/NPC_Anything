namespace NpcAnything.GodotAdapter;

/// <summary>
/// Reproducible run metadata shared by the demo and Godot adapter.
/// </summary>
public static class GodotRunConfiguration
{
    public static string ScenarioId { get; private set; } = "SC02_CORE";
    public static int Seed { get; private set; } = 42;

    public static void Configure(string scenarioId, int seed)
    {
        ScenarioId = scenarioId;
        Seed = seed;
    }
}
