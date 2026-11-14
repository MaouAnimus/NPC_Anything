using System.Collections.Generic;
using System.Linq;
using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Memory;
using NpcAnything.Core.Ports;
using NpcAnything.Core.Rules;
using NpcAnything.Core.Utility;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Controlador del cerebro táctico de un NPC.
/// Conecta el bucle de Godot con NpcAnything.Core a una frecuencia controlada (ej. 250 ms),
/// garantizando que el Core decida sin ensuciar el adaptador con reglas de conveniencia.
/// </summary>
public partial class NpcBrainController : Node
{
    [Export] public string NpcId { get; set; } = "enemy_advanced_01";
    [Export] public NpcRole Role { get; set; } = NpcRole.Assault;
    [Export] public float DecisionIntervalSeconds { get; set; } = 0.25f; // 4 Hz
    [Export] public float MaxHealth { get; set; } = 100.0f;

    public float CurrentHealth { get; private set; }

    private CharacterBody3D? _actor;
    private NpcMotorController? _motor;
    private RayCast3D? _lineOfSightRay;

    private MemoryStore _memoryStore = null!;
    private UtilitySelector _utilitySelector = null!;
    private GodotWorldSensorPort _sensorPort = null!;
    private GodotSpatialQueryPort _spatialPort = null!;
    private GodotActionExecutorPort _actionExecutor = null!;

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

        // Inicializar componentes del Core
        _memoryStore = new MemoryStore(defaultTtlMs: 4000);
        _utilitySelector = new UtilitySelector(hysteresisMargin: 0.12f, minimumViableScore: 0.15f);

        // Inicializar puertos de adaptación
        _sensorPort = new GodotWorldSensorPort(_actor, _lineOfSightRay);
        _spatialPort = new GodotSpatialQueryPort(_actor);
        _actionExecutor = new GodotActionExecutorPort(_actor, _motor, OnExecuteAttack);
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

        // 1. Recolección sensorial física -> Estímulos puros
        var stimuli = _sensorPort.Observe(NpcId, nowMs);

        // 2. Actualización de hechos en la memoria de trabajo con degradación temporal
        _memoryStore.ProcessStimuli(stimuli, nowMs);
        var memorySnapshot = _memoryStore.GetActiveFacts(nowMs);

        // 3. Consulta de geometría y navegación -> Coberturas transitables
        var reachableCovers = _spatialPort.QueryReachableCovers(NpcId, _actor.GlobalPosition.ToVec3(), maxRadiusMeters: 16.0f);

        // 4. Construcción del DTO contextual (sin datos omniscientes)
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

        // 5. Generación de candidatos de acción a partir del conocimiento disponible
        var candidates = GenerateActionCandidates(context);

        // 6. Filtrado estricto por reglas de justicia (anti-wallhack y respeto de coberturas)
        var validCandidates = FairnessFilter.ApplyFairnessRules(candidates, context);

        // 7. Selección por función de utilidad con histéresis y fallback
        var decision = _utilitySelector.SelectBestAction(validCandidates, context);
        _currentDecision = decision;

        // 8. Solicitud al ejecutor físico
        _actionExecutor.Request(NpcId, decision);
    }

    private List<ActionCandidate> GenerateActionCandidates(NpcContext context)
    {
        var list = new List<ActionCandidate>();

        // Verificar si existe memoria activa del jugador
        var playerFact = context.Memory.Values.FirstOrDefault(f => f.SubjectId == "Player");

        if (playerFact != null && !playerFact.IsExpired(context.CurrentTimeMs))
        {
            float dist = context.Position.DistanceTo(playerFact.EstimatedPosition);
            float confidence = playerFact.GetDecayedConfidence(context.CurrentTimeMs);

            // Candidato de ataque
            if (dist <= 14.0f)
            {
                float attackScore = (1.0f - (dist / 14.0f)) * 0.7f + (confidence * 0.3f);
                list.Add(new ActionCandidate(
                    ActionKind.Attack,
                    attackScore,
                    "TARGET_IN_RANGE_AND_SIGHT",
                    TargetId: "Player",
                    TargetPosition: playerFact.EstimatedPosition));
            }

            // Candidato de cobertura si la salud es baja o la amenaza es alta
            if (context.ReachableCoverPoints.Count > 0)
            {
                Vec3 bestCover = context.ReachableCoverPoints[0];
                float coverScore = (1.0f - context.HealthRatio) * 0.8f + 0.3f;
                list.Add(new ActionCandidate(
                    ActionKind.TakeCover,
                    coverScore,
                    "THREAT_ACTIVE_COVER_SEEKING",
                    TargetPosition: bestCover));
            }

            // Candidato de investigación si la confianza está degradándose
            if (confidence < 0.8f)
            {
                list.Add(new ActionCandidate(
                    ActionKind.Investigate,
                    0.55f,
                    "CONFIDENCE_DEGRADED_SEARCH_LAST_SEEN",
                    TargetPosition: playerFact.EstimatedPosition));
            }
        }
        else
        {
            // Sin contacto: mantener posición o patrulla
            list.Add(new ActionCandidate(ActionKind.Hold, 0.40f, "NO_ACTIVE_CONTACT_HOLD"));
        }

        return list;
    }

    private void OnExecuteAttack(string targetId)
    {
        if (_attackCooldown <= 0f && _actor != null)
        {
            _attackCooldown = 1.0f;
            var target = _actor.GetTree().GetFirstNodeInGroup("Player") as Node;
            if (target != null && target.HasMethod("ApplyDamage"))
            {
                target.Call("ApplyDamage", 12.0f);
                GD.Print($"[NpcBrainController {NpcId}] ¡Disparo certero sobre {targetId}!");
            }
        }
    }

    public void ApplyDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
    }
}
