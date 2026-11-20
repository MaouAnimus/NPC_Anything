using System.Collections.Generic;
using Godot;
using NpcAnything.Core.Configuration;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Memory;
using NpcAnything.Core.Ports;
using NpcAnything.Core.Rules;
using NpcAnything.Core.Tactics;
using NpcAnything.Core.Utility;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Orquestador del cerebro de un NPC en Godot.
/// REGLA ARQUITECTÓNICA (P1): Cero lógica táctica dentro de este componente.
/// Se limita a orquestar sensores -> memoria -> ActionCandidateGenerator (Core) -> UtilitySelector (Core) -> ejecutor.
/// </summary>
public partial class NpcBrainController : Node
{
    [Export] public string NpcId { get; set; } = "enemy_core_01";
    [Export] public string SquadId { get; set; } = "squad_alpha";
    [Export] public NpcRole Role { get; set; } = NpcRole.Assault;
    [Export] public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;
    [Export] public float DecisionIntervalSeconds { get; set; } = 0.25f; // 4 Hz
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public float AttackDamage { get; set; } = 10.0f;
    [Export] public Godot.Collections.Array<Vector3> PatrolWaypoints { get; set; } = new();

    public float CurrentHealth { get; private set; }

    private CharacterBody3D? _actor;
    private NpcMotorController? _motor;
    private RayCast3D? _lineOfSightRay;

    // Componentes puros del Core
    private DifficultyProfile _difficultyProfile = DifficultyProfile.Default;
    private MemoryStore _memoryStore = null!;
    private ActionCandidateGenerator _candidateGenerator = null!;
    private UtilitySelector _utilitySelector = null!;

    // Puertos del adaptador
    private GodotWorldSensorPort _sensorPort = null!;
    private GodotSpatialQueryPort _spatialPort = null!;
    private GodotActionExecutorPort _actionExecutor = null!;
    private ITelemetryExporter _telemetryExporter = null!;
    private ISquadMessageBus? _squadMessageBus;

    private float _decisionTimer = 0f;
    private ActionDecision _currentDecision = ActionDecision.FallbackHold();
    private float _attackCooldown = 0f;

    public override void _Ready()
    {
        _actor = GetParentOrNull<CharacterBody3D>();
        if (_actor == null)
        {
            GD.PushError($"[NpcBrainController] Debe ser hijo de un CharacterBody3D.");
            return;
        }

        _actor.AddToGroup("Enemies");
        CurrentHealth = MaxHealth;

        _motor = _actor.GetNodeOrNull<NpcMotorController>("NpcMotorController");
        if (_motor == null)
        {
            _motor = new NpcMotorController();
            _motor.Name = "NpcMotorController";
            _actor.AddChild(_motor);
        }

        _lineOfSightRay = _actor.GetNodeOrNull<RayCast3D>("LineOfSightRay");

        if (PatrolWaypoints.Count == 0 && _actor != null)
        {
            PatrolWaypoints.Add(_actor.GlobalPosition + new Vector3(8, 0, 0));
            PatrolWaypoints.Add(_actor.GlobalPosition + new Vector3(-8, 0, 0));
        }

        // 0. Calibrar perfil de dificultad (BK-023, RF-11, RF-18)
        DifficultyLevel effectiveLevel = (GodotRunConfiguration.Difficulty != DifficultyLevel.Normal)
            ? GodotRunConfiguration.Difficulty
            : Difficulty;
        _difficultyProfile = DifficultyProfile.ForLevel(effectiveLevel);
        DecisionIntervalSeconds = _difficultyProfile.ReactionDelaySeconds;

        // 1. Inicializar componentes puros del Core
        _memoryStore = new MemoryStore(defaultTtlMs: _difficultyProfile.MemoryTtlMs);
        _candidateGenerator = new ActionCandidateGenerator { MaxAttackRange = 12.0f, MinVisualConfidenceToAttack = 0.70f };
        _utilitySelector = new UtilitySelector(hysteresisMargin: 0.12f, minimumViableScore: 0.15f);

        // 2. Inicializar adaptadores de puertos
        _telemetryExporter = new GodotTelemetryExporter(this);
        _sensorPort = new GodotWorldSensorPort(
            _actor!,
            _lineOfSightRay,
            maxVisionDistance: 20.0f,
            visionHalfAngleDegrees: 45.0f,
            randomSeed: (uint)GodotRunConfiguration.Seed,
            soundLocationErrorMeters: _difficultyProfile.SoundLocationErrorMeters);
        _spatialPort = new GodotSpatialQueryPort(_actor!);
        _squadMessageBus = GodotSquadService.GetOrCreateSquadBus(SquadId);
        _actionExecutor = new GodotActionExecutorPort(
            _actor!,
            _motor,
            _lineOfSightRay,
            ApplyDamageToTarget,
            attackDamage: AttackDamage,
            maxAttackRange: 12.0f,
            visionHalfAngleDegrees: 45.0f,
            onTelemetryEvent: (evt, act, sc, reason) => EmitTelemetry(evt, act, sc, reason));

        _motor.OnDestinationSet = (pos, mode) =>
        {
            EmitTelemetry("navigation_started", mode.ToString(), 1.0f, $"NAV_TO_{pos.X:F1}_{pos.Z:F1}");
        };

        _motor.OnDestinationReached = (pos) =>
        {
            EmitTelemetry("navigation_completed", "Arrived", 1.0f, $"ARRIVED_{pos.X:F1}_{pos.Z:F1}");
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GodotNavigationReadiness.IsReady)
        {
            _decisionTimer = 0f;
            return;
        }

        if (_attackCooldown > 0f) _attackCooldown -= (float)delta;

        _decisionTimer += (float)delta;
        if (_decisionTimer >= DecisionIntervalSeconds)
        {
            _decisionTimer = 0f;
            ExecuteDecisionCycle();
        }
    }

    private void ExecuteDecisionCycle()
    {
        if (!GodotNavigationReadiness.IsReady || _actor == null || !_actor.IsInsideTree()) return;

        // P1-04: Impedir que el planificador tome decisiones si la navegación espacial no está sincronizada
        if (!_spatialPort.IsNavigationReady) return;

        long nowMs = (long)Time.GetTicksMsec();

        // 1. Sensores físicos -> Estímulos
        var stimuli = new List<Stimulus>(_sensorPort.Observe(NpcId, nowMs));

        // 2. Recepción de mensajes de escuadrón con retardo y alcance (BK-020, BK-021)
        if (_squadMessageBus != null)
        {
            var incoming = _squadMessageBus.ReceiveMessages(NpcId, SquadId, nowMs, _actor.GlobalPosition.ToVec3());
            foreach (var msg in incoming)
            {
                if (msg.MessageType == "CONTACT_ESTIMATED" && msg.TargetPosition.HasValue)
                {
                    stimuli.Add(new Stimulus(
                        SubjectId: "Player",
                        Kind: StimulusType.SquadMessage,
                        EstimatedPosition: msg.TargetPosition.Value,
                        Confidence: msg.Confidence * (0.6f + _difficultyProfile.CoordinationWeight * 0.4f),
                        PrecisionMeters: 1.5f,
                        ObservedAtMs: nowMs,
                        SourceId: msg.SenderId));
                }
            }
        }

        // 3. Memoria con degradación temporal
        _memoryStore.ProcessStimuli(stimuli, nowMs);

        var memorySnapshot = _memoryStore.GetActiveFacts(nowMs);

        // 3. Consulta de geometría y rutas navegables (ISpatialQueryPort con MapGetPath)
        var reachableCovers = _spatialPort.QueryReachableCovers(NpcId, _actor.GlobalPosition.ToVec3(), maxRadiusMeters: 16.0f);

        // 4. Mapear waypoints de patrulla al Core
        var waypoints = new List<Vec3>();
        foreach (var wp in PatrolWaypoints)
        {
            waypoints.Add(wp.ToVec3());
        }

        // 5. Mapear aliados conocidos en la escena (jugador para Companion, otros enemigos para escuadrón)
        var allyPositions = new List<Vec3>();
        if (_actor.IsInsideTree())
        {
            string groupName = (Role == NpcRole.Companion) ? "Player" : "Enemies";
            foreach (var node in _actor.GetTree().GetNodesInGroup(groupName))
            {
                if (node is CharacterBody3D body && body != _actor)
                {
                    allyPositions.Add(body.GlobalPosition.ToVec3());
                }
            }
        }

        // Obtener reservas activas de cobertura del escuadrón (BK-014)
        var reservedCovers = _squadMessageBus?.GetReservedCovers(SquadId, nowMs) ?? new HashSet<string>();

        // 6. Construcción del DTO de contexto
        var context = new NpcContext(
            NpcId: NpcId,
            Role: Role,
            Position: _actor.GlobalPosition.ToVec3(),
            HealthRatio: CurrentHealth / MaxHealth,
            CurrentAction: _currentDecision.Kind,
            CurrentActionScore: _currentDecision.Score,
            CurrentTimeMs: nowMs,
            Memory: memorySnapshot,
            ReachableCoverPoints: reachableCovers,
            ReservedSquadCovers: reservedCovers,
            PatrolWaypoints: waypoints,
            KnownAllyPositions: allyPositions,
            Difficulty: _difficultyProfile.Level);

        // 6. Generación de candidatos tácticamente pura (NpcAnything.Core)
        var rawCandidates = _candidateGenerator.GenerateCandidates(context);

        // 7. Reglas de justicia anti-trampas (NpcAnything.Core)
        var validCandidates = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        // 8. Selección por función de utilidad con histéresis (NpcAnything.Core)
        var decision = _utilitySelector.SelectBestAction(validCandidates, context);

        // Registrar telemetría si hubo cambio de acción o score significativo
        if (decision.Kind != _currentDecision.Kind)
        {
            EmitTelemetry("action_changed", decision.Kind.ToString(), decision.Score, decision.ReasonCode);
        }

        _currentDecision = decision;

        // Arbitraje de cobertura: reservar punto en el escuadrón
        if ((decision.Kind == ActionKind.TakeCover || decision.Kind == ActionKind.Flank) && decision.TargetPosition.HasValue)
        {
            string coverKey = $"{decision.TargetPosition.Value.X:F1}_{decision.TargetPosition.Value.Z:F1}";
            _squadMessageBus?.TryReserveCover(SquadId, coverKey, NpcId, nowMs, durationMs: 4000);
        }

        // Difusión de contacto visual a los aliados modulada por la dificultad y latencia del perfil
        var targetFact = memorySnapshot.Values.FirstOrDefault(f => !f.IsExpired(nowMs));
        if (_squadMessageBus != null && targetFact != null && targetFact.Provenance.Contains("vision", StringComparison.OrdinalIgnoreCase) && targetFact.GetDecayedConfidence(nowMs) >= 0.90f)
        {
            _squadMessageBus.PostMessage(new SquadMessage(
                SenderId: NpcId,
                SquadId: SquadId,
                MessageType: "CONTACT_ESTIMATED",
                TargetPosition: targetFact.EstimatedPosition,
                Confidence: targetFact.GetDecayedConfidence(nowMs),
                SentAtMs: nowMs,
                DeliverAtMs: nowMs + _difficultyProfile.SquadMessageDelayMs,
                ExpiresAtMs: nowMs + _difficultyProfile.MemoryTtlMs,
                SenderPosition: _actor.GlobalPosition.ToVec3(),
                MaxRangeMeters: 30.0f));
        }

        // 9. Ejecución física con verificación de trayectoria
        if (decision.Kind == ActionKind.Attack && _attackCooldown > 0f)
        {
            // Respetar cadencia de disparo
            return;
        }

        _actionExecutor.Request(NpcId, decision);

        if (decision.Kind == ActionKind.Attack)
        {
            _attackCooldown = 1.0f;
        }
    }

    private void EmitTelemetry(string eventType, string action, float score, string reasonCode)
    {
        long nowMs = (long)Time.GetTicksMsec();
        _telemetryExporter.Emit(new TelemetryEvent(
            SchemaVersion: "0.1",
            RunId: "run_active",
            ScenarioId: GodotRunConfiguration.ScenarioId,
            Seed: GodotRunConfiguration.Seed,
            SimulationTimeMs: nowMs,
            NpcId: NpcId,
            Event: eventType,
            Role: Role.ToString(),
            Action: action,
            Score: score,
            ReasonCode: reasonCode,
            KnowledgeSource: "core_utility",
            KnowledgeAgeMs: 0,
            ProfileHash: "sha256_mock"));
    }

    private void ApplyDamageToTarget(string targetId, float damage)
    {
        if (_actor == null || !_actor.IsInsideTree()) return;

        Node? foundNode = null;
        var playerGroup = _actor.GetTree().GetNodesInGroup("Player");
        foreach (var node in playerGroup)
        {
            if (node.Name == targetId || node.GetPath().ToString() == targetId)
            {
                foundNode = node;
                break;
            }
        }

        if (foundNode != null && foundNode.HasMethod("ApplyDamage"))
        {
            foundNode.Call("ApplyDamage", damage);
        }
        else
        {
            GD.PushWarning($"[NpcBrainController] ApplyDamageToTarget: objetivo '{targetId}' no encontrado o no tiene ApplyDamage.");
        }
    }

    public void ApplyDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        EmitTelemetry("damage_received", "Hurt", amount, $"HEALTH_{CurrentHealth:F1}");
    }
}
