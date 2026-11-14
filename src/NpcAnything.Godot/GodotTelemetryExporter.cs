using System;
using Godot;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador de telemetría que enlaza las trazas del Core con el registrador de la escena (ArenaManager).
/// </summary>
public class GodotTelemetryExporter : ITelemetryExporter
{
    private readonly Node _sceneContext;

    public GodotTelemetryExporter(Node sceneContext)
    {
        _sceneContext = sceneContext;
    }

    public void Emit(TelemetryEvent evt)
    {
        if (!_sceneContext.IsInsideTree()) return;

        var arenaManager = _sceneContext.GetTree().Root.FindChild("ArenaManager", recursive: true, owned: false)
                           ?? _sceneContext.GetTree().CurrentScene;

        if (arenaManager != null && arenaManager.HasMethod("LogEvent"))
        {
            arenaManager.Call("LogEvent", evt.Event, evt.NpcId, evt.Action, evt.Score, evt.ReasonCode);
        }
    }
}
