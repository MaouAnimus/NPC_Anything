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

            case ActionKind.Investigate:
            case ActionKind.TakeCover:
            case ActionKind.Flank:
            case ActionKind.Regroup:
                if (decision.TargetPosition.HasValue)
                {
                    _motor.SetDestination(decision.TargetPosition.Value.ToGodot(), MovementMode.Sprint);
                }
                break;

            case ActionKind.Attack:
                _motor.Stop();
                if (decision.TargetPosition.HasValue)
                {
                    _motor.FaceTowards(decision.TargetPosition.Value.ToGodot());
                }

                if (!string.IsNullOrEmpty(decision.TargetId))
                {
                    ExecutePhysicalAttack(decision.TargetId);
                }
                break;

            case ActionKind.Hold:
            default:
                _motor.Stop();
                break;
        }
    }

    private void ExecutePhysicalAttack(string targetId)
    {
        if (!_actor.IsInsideTree()) return;

        var tree = _actor.GetTree();

        // 1. Resolver el objetivo concreto correspondiente a targetId
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

        // Fallback si el ID no coincidió con el nombre del nodo pero hay un jugador en el grupo
        if (foundNode == null && playerGroup.Count > 0)
        {
            foundNode = playerGroup[0];
        }

        if (foundNode is not CharacterBody3D targetNode)
        {
            GD.PushWarning($"[GodotActionExecutor] Objetivo '{targetId}' no encontrado en el árbol de escena.");
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

        // 3. Re-validación estricta de cono de visión (FOV)
        Vector3 forward = -_actor.Transform.Basis.Z;
        Vector3 horizDir = new Vector3(toTarget.X, 0f, toTarget.Z);
        if (horizDir.LengthSquared() > 0.001f)
        {
            float angle = Mathf.RadToDeg(forward.AngleTo(horizDir.Normalized()));
            if (angle > _visionHalfAngleDegrees)
            {
                _onTelemetryEvent?.Invoke("attack_blocked", "Attack", 0f, "OUT_OF_FOV");
                GD.Print($"[GodotActionExecutor] Disparo descartado: objetivo fuera de cono de visión ({angle:F1}° > {_visionHalfAngleDegrees}°).");
                return;
            }
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
