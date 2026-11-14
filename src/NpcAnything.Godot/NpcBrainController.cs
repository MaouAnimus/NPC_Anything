using System.Collections.Generic;
using Godot;
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
    [Export] public NpcRole Role { get; set; } = NpcRole.Assault;
    [Export] public float DecisionIntervalSeconds { get; set; } = 0.25f; // 4 Hz
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public float AttackDamage { get; set; } = 10.0f;

    public float CurrentHealth { get; private set; }

    private CharacterBody3D? _actor;
    private NpcMotorController? _motor;
    private RayCast3D? _lineOfSightRay;

    // Componentes puros del Core
    private MemoryStore _memoryStore = null!;
    private ActionCandidateGenerator _candidateGenerator = null!;
    private UtilitySelector _utilitySelector = null!;

    // Puertos del adaptador
    private GodotWorldSensorPort _sensorPort = null!;
    private GodotSpatialQueryPort _spatialPort = null!;
    private GodotActionExecutorPort _actionExecutor = null!;
    private ITelemetryExporter _telemetryExporter = null!;

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

        // 1. Inicializar componentes puros del Core
        _memoryStore = new MemoryStore(defaultTtlMs: 4000);
        _candidateGenerator = new ActionCandidateGenerator { MaxAttackRange = 12.0f, MinVisualConfidenceToAttack = 0.70f };
        _utilitySelector = new UtilitySelector(hysteresisMargin: 0.12f, minimumViableScore: 0.15f);

        // 2. Inicializar adaptadores de puertos
        _sensorPort = new GodotWorldSensorPort(_actor, _lineOfSightRay, maxVisionDistance: 20.0f, visionHalfAngleDegrees: 45.0f);
        _spatialPort = new GodotSpatialQueryPort(_actor);
        _actionExecutor = new GodotActionExecutorPort(_actor, _motor, _lineOfSightRay, ApplyDamageToTarget, AttackDamage);
        _telemetryExporter = new GodotTelemetryExporter(this);
    }

    public override void _PhysicsProcess(double delta)
    {
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
        if (_actor == null || !_actor.IsInsideTree()) return;

        long nowMs = (long)Time.GetTicksMsec();

        // 1. Sensores físicos -> Estímulos
        var stimuli = _sensorPort.Observe(NpcId, nowMs);

        // 2. Memoria con degradación temporal
        _memoryStore.ProcessStimuli(stimuli, nowMs);
        var memorySnapshot = _memoryStore.GetActiveFacts(nowMs);

        // 3. Consulta de geometría y rutas navegables (ISpatialQueryPort con MapGetPath)
        var reachableCovers = _spatialPort.QueryReachableCovers(NpcId, _actor.GlobalPosition.ToVec3(), maxRadiusMeters: 16.0f);

        // 4. Construcción del DTO de contexto
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
            ReservedSquadCovers: new HashSet<string>());

        // 5. Generación de candidatos tácticamente pura (NpcAnything.Core)
        var rawCandidates = _candidateGenerator.GenerateCandidates(context);

        // 6. Reglas de justicia anti-trampas (NpcAnything.Core)
        var validCandidates = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        // 7. Selección por función de utilidad con histéresis (NpcAnything.Core)
        var decision = _utilitySelector.SelectBestAction(validCandidates, context);

        // Registrar telemetría si hubo cambio de acción o score significativo
        if (decision.Kind != _currentDecision.Kind)
        {
            _telemetryExporter.Emit(new TelemetryEvent(
                SchemaVersion: "0.1",
                RunId: "run_active",
                ScenarioId: "SC02_CORE",
                Seed: 42,
                SimulationTimeMs: nowMs,
                NpcId: NpcId,
                Event: "action_changed",
                Role: Role.ToString(),
                Action: decision.Kind.ToString(),
                Score: decision.Score,
                ReasonCode: decision.ReasonCode,
                KnowledgeSource: "core_utility",
                KnowledgeAgeMs: 0,
                ProfileHash: "sha256_mock"));
        }

        _currentDecision = decision;

        // 8. Ejecución física con verificación de trayectoria
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

    private void ApplyDamageToTarget(string targetId, float damage)
    {
        if (_actor == null || !_actor.IsInsideTree()) return;

        var target = _actor.GetTree().GetFirstNodeInGroup("Player") as Node;
        if (target != null && target.HasMethod("ApplyDamage"))
        {
            target.Call("ApplyDamage", damage);
        }
    }

    public void ApplyDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
    }
}
