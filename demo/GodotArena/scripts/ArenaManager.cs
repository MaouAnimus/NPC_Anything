using Godot;
using System.Text.Json;

namespace GodotArena.Scripts;

/// <summary>
/// Coordinador de la escena de prueba y registrador de telemetría experimental.
/// </summary>
public partial class ArenaManager : Node3D
{
    [Export] public string ScenarioId { get; set; } = "SC-02";
    [Export] public int RandomSeed { get; set; } = 42;
    [Export] public bool EnableTelemetryLogging { get; set; } = true;

    private string _runId = "";
    private string _logFilePath = "";
    private long _simulationStartMs = 0;

    public override void _Ready()
    {
        _simulationStartMs = (long)Time.GetTicksMsec();
        _runId = $"run_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{RandomSeed}";

        GD.Seed((ulong)RandomSeed);

        if (EnableTelemetryLogging)
        {
            string projectRoot = ProjectSettings.GlobalizePath("res://../../");
            string runsDir = System.IO.Path.Combine(projectRoot, "runs");
            System.IO.Directory.CreateDirectory(runsDir);
            _logFilePath = System.IO.Path.Combine(runsDir, $"{_runId}.jsonl");

            LogEvent("arena_started", "system", "Initialize", 1.0f, "SCENARIO_READY");
            GD.Print($"[ArenaManager] Telemetría iniciada en: {_logFilePath}");
        }
    }

    public void LogEvent(string eventType, string actorId, string action, float score, string reasonCode)
    {
        if (!EnableTelemetryLogging || string.IsNullOrEmpty(_logFilePath)) return;

        long currentMs = (long)Time.GetTicksMsec() - _simulationStartMs;

        var payload = new
        {
            schema_version = "0.1",
            run_id = _runId,
            scenario_id = ScenarioId,
            seed = RandomSeed,
            simulation_time_ms = currentMs,
            npc_id = actorId,
            @event = eventType,
            action = action,
            score = score,
            reason_code = reasonCode
        };

        string line = JsonSerializer.Serialize(payload) + "\n";
        System.IO.File.AppendAllText(_logFilePath, line);
    }
}
