using System;
using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador del puerto de ejecución de acciones.
/// Recibe decisiones del Core y las delega al NpcMotorController o a los efectores de combate.
/// REGLA ESTRICTA (P0): Un ataque físico requiere comprobación estricta de trayectoria libre de obstáculos
/// en el instante exacto de ejecución. Prohibido aplicar daño a través de muros.
/// </summary>
public class GodotActionExecutorPort : IActionExecutorPort
{
    private readonly CharacterBody3D _actor;
    private readonly NpcMotorController _motor;
    private readonly RayCast3D? _lineOfSightRay;
    private readonly Action<string, float>? _onAttackDamageApplied;
    private readonly Action<string, string, float, string>? _onTelemetryEvent;
    private readonly float _attackDamage;
    private readonly float _maxAttackRange;
    private readonly float _visionHalfAngleDegrees;

    public GodotActionExecutorPort(
        CharacterBody3D actor,
        NpcMotorController motor,
        RayCast3D? lineOfSightRay = null,
        Action<string, float>? onAttackDamageApplied = null,
        float attackDamage = 10.0f,
        float maxAttackRange = 12.0f,
        float visionHalfAngleDegrees = 45.0f,
        Action<string, string, float, string>? onTelemetryEvent = null)
    {
        _actor = actor;
        _motor = motor;
        _lineOfSightRay = lineOfSightRay;
        _onAttackDamageApplied = onAttackDamageApplied;
        _attackDamage = attackDamage;
        _maxAttackRange = maxAttackRange;
        _visionHalfAngleDegrees = visionHalfAngleDegrees;
        _onTelemetryEvent = onTelemetryEvent;
    }

    public bool CanExecute(string npcId, ActionKind action)
    {
        return _actor.IsInsideTree();
    }

    public void Request(string npcId, ActionDecision decision)
    {
        if (!CanExecute(npcId, decision.Kind)) return;

        switch (decision.Kind)
        {
            case ActionKind.Patrol:
                if (decision.TargetPosition.HasValue)
                {
                    _motor.SetDestination(decision.TargetPosition.Value.ToGodot(), MovementMode.Walk);
                }
                break;

            case ActionKind.Pursue:
            case ActionKind.Investigate:
            case ActionKind.TakeCover:
            case ActionKind.Regroup:
                if (decision.TargetPosition.HasValue)
                {
                    _motor.SetDestination(decision.TargetPosition.Value.ToGodot(), MovementMode.Sprint);
                }
                break;

            case ActionKind.Flank:
                if (decision.TargetPosition.HasValue)
                {
                    _motor.SetDestination(decision.TargetPosition.Value.ToGodot(), MovementMode.Sprint);
                    _onTelemetryEvent?.Invoke("flank_initiated", "Flank", decision.Score, decision.ReasonCode);
                }
                break;

            case ActionKind.Attack:
                _motor.Stop();
                if (!string.IsNullOrEmpty(decision.TargetId))
                {
                    ExecutePhysicalAttack(decision.TargetId, decision.TargetPosition?.ToGodot());
                }
                break;

            case ActionKind.Hold:
            default:
                _motor.Stop();
                break;
        }
    }

    private void ExecutePhysicalAttack(string targetId, Vector3? targetPosition = null)
    {
        if (!_actor.IsInsideTree()) return;

        var tree = _actor.GetTree();

        // 1. Resolver el objetivo concreto correspondiente a targetId sin fallbacks ciegos (P1-02)
        Node? foundNode = null;
        var playerGroup = tree.GetNodesInGroup("Player");
        foreach (var node in playerGroup)
        {
            if (node.Name == targetId || node.GetPath().ToString() == targetId)
            {
                foundNode = node;
                break;
            }
        }

        if (foundNode is not CharacterBody3D targetNode)
        {
            _onTelemetryEvent?.Invoke("attack_blocked", "Attack", 0f, "TARGET_NOT_FOUND");
            GD.PushWarning($"[GodotActionExecutor] Objetivo '{targetId}' no encontrado en el grupo Player. Ataque descartado.");
            return;
        }

        Vector3 startPos = _actor.GlobalPosition + new Vector3(0f, 1.2f, 0f);
        Vector3 targetPos = targetNode.GlobalPosition + new Vector3(0f, 1.0f, 0f);
        Vector3 toTarget = targetPos - startPos;
        float distance = toTarget.Length();

        // 2. Re-validación estricta de alcance físico
        if (distance > _maxAttackRange)
        {
            _onTelemetryEvent?.Invoke("attack_blocked", "Attack", 0f, "OUT_OF_RANGE");
            GD.Print($"[GodotActionExecutor] Disparo descartado: objetivo fuera de alcance ({distance:F1}m > {_maxAttackRange}m).");
            return;
        }

        // 3. Re-validación estricta de cono de visión (FOV) PREVIA a la rotación (P1-01)
        Vector3 forward = -_actor.Transform.Basis.Z;
        Vector3 horizDir = new Vector3(toTarget.X, 0f, toTarget.Z);
        float currentAngle = horizDir.LengthSquared() > 0.001f
            ? Mathf.RadToDeg(forward.AngleTo(horizDir.Normalized()))
            : 0f;

        // Iniciar orientación del motor hacia el objetivo
        if (targetPosition.HasValue)
        {
            _motor.FaceTowards(targetPosition.Value);
        }
        else
        {
            _motor.FaceTowards(targetPos);
        }

        // Si no estaba ya orientado en el cono FOV (45°), aplazar el disparo mientras adquiere ángulo
        if (currentAngle > _visionHalfAngleDegrees)
        {
            _onTelemetryEvent?.Invoke("attack_blocked", "Attack", 0f, "ACQUIRING_TARGET_ANGLE");
            GD.Print($"[GodotActionExecutor] Disparo en espera: adquiriendo ángulo hacia {targetId} ({currentAngle:F1}° > {_visionHalfAngleDegrees}°).");
            return;
        }

        // 4. Validación física de trayectoria obligatoria (P0 anti-wallhack)
        bool hasClearTrajectory = false;

        if (_lineOfSightRay != null)
        {
            _lineOfSightRay.GlobalPosition = startPos;
            _lineOfSightRay.TargetPosition = _lineOfSightRay.ToLocal(targetPos);
            _lineOfSightRay.ForceRaycastUpdate();

            if (_lineOfSightRay.IsColliding())
            {
                var collider = _lineOfSightRay.GetCollider() as Node;
                hasClearTrajectory = (collider != null && (collider == targetNode || collider.IsInGroup("Player")));
            }
        }
        else
        {
            var spaceState = _actor.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(startPos, targetPos);
            query.CollisionMask = 1 | 2; // Geometría del mundo y jugador
            var hit = spaceState.IntersectRay(query);

            if (hit.Count > 0 && hit.TryGetValue("collider", out var colObj))
            {
                var collider = colObj.AsGodotObject() as Node;
                hasClearTrajectory = (collider != null && (collider == targetNode || collider.IsInGroup("Player")));
            }
        }

        if (hasClearTrajectory)
        {
            _onAttackDamageApplied?.Invoke(targetId, _attackDamage);
            _onTelemetryEvent?.Invoke("attack_executed", "Attack", _attackDamage, "HIT_CONFIRMED");
            GD.Print($"[GodotActionExecutor] Disparo con trayectoria libre conectado en {targetId} por {_attackDamage} daño.");
        }
        else
        {
            _onTelemetryEvent?.Invoke("attack_blocked", "Attack", 0f, "OCCLUDED_BY_GEOMETRY");
            GD.Print($"[GodotActionExecutor] Disparo bloqueado por oclusión física (muro/cobertura). Daño evitado.");
        }
    }

    public void Cancel(string npcId, string reason)
    {
        _motor.Stop();
        GD.Print($"[ActionExecutor {npcId}] Acción cancelada: {reason}");
    }
}
