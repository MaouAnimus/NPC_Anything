namespace NpcAnything.GodotAdapter;

/// <summary>
/// Shared navigation readiness signal for the Godot adapter and its host scene.
/// </summary>
public static class GodotNavigationReadiness
{
    public static bool IsReady { get; private set; }

    public static void SetReady(bool isReady)
    {
        IsReady = isReady;
    }
}
