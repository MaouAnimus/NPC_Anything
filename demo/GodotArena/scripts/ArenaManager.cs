using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using NpcAnything.GodotAdapter;

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
    private bool _hasCustomTelemetryPath;

    public override void _EnterTree()
    {
        if (!TryApplyCommandLineOverrides(out string error))
        {
            GD.PushError($"[ArenaManager] {error}");
            GetTree().Quit(2);
            return;
        }

        GD.Seed((ulong)RandomSeed);
        GodotRunConfiguration.Configure(ScenarioId, RandomSeed);
    }

    public override void _Ready()
    {
        _simulationStartMs = (long)Time.GetTicksMsec();
        _runId = $"run_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{RandomSeed}";

        if (EnableTelemetryLogging)
        {
            if (!_hasCustomTelemetryPath)
            {
                string projectRoot = ProjectSettings.GlobalizePath("res://../../");
                string runsDir = Path.Combine(projectRoot, "runs");
                _logFilePath = Path.Combine(runsDir, $"{_runId}.jsonl");
            }

            string? outputDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            LogEvent("arena_started", "system", "Initialize", 1.0f, "SCENARIO_READY");
            GD.Print($"[ArenaManager] Telemetría iniciada en: {_logFilePath}");
        }
    }

    private bool TryApplyCommandLineOverrides(out string error)
    {
        error = "";
        var seenOptions = new HashSet<string>();
        string[] arguments = OS.GetCmdlineUserArgs();

        for (int i = 0; i < arguments.Length; i++)
        {
            string option = arguments[i];
            if (option is not "--scenario-id" and not "--seed" and not "--telemetry-path")
            {
                error = $"Argumento de usuario desconocido: '{option}'.";
                return false;
            }

            if (!seenOptions.Add(option))
            {
                error = $"Argumento duplicado: '{option}'.";
                return false;
            }

            if (++i >= arguments.Length)
            {
                error = $"Falta el valor para '{option}'.";
                return false;
            }

            string value = arguments[i];
            switch (option)
            {
                case "--scenario-id":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        error = "El escenario no puede estar vacío.";
                        return false;
                    }

                    ScenarioId = value;
                    break;
                case "--seed":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int seed))
                    {
                        error = $"Semilla inválida: '{value}'. Debe ser un entero no negativo.";
                        return false;
                    }

                    RandomSeed = seed;
                    break;
                case "--telemetry-path":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        error = "La ruta de telemetría no puede estar vacía.";
                        return false;
                    }

                    _logFilePath = Path.GetFullPath(value);
                    _hasCustomTelemetryPath = true;
                    break;
            }
        }

        return true;
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
