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
    private readonly float _attackDamage;

    public GodotActionExecutorPort(
        CharacterBody3D actor,
        NpcMotorController motor,
        RayCast3D? lineOfSightRay = null,
        Action<string, float>? onAttackDamageApplied = null,
        float attackDamage = 10.0f)
    {
        _actor = actor;
        _motor = motor;
        _lineOfSightRay = lineOfSightRay;
        _onAttackDamageApplied = onAttackDamageApplied;
        _attackDamage = attackDamage;
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
            case ActionKind.TakeCover:
            case ActionKind.Flank:
            case ActionKind.Regroup:
            case ActionKind.Investigate:
            case ActionKind.Patrol:
                if (decision.TargetPosition.HasValue)
                {
                    _motor.SetDestination(decision.TargetPosition.Value.ToGodot());
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
        var targetNode = tree.GetFirstNodeInGroup("Player") as CharacterBody3D;
        if (targetNode == null) return;

        Vector3 startPos = _actor.GlobalPosition + new Vector3(0f, 1.2f, 0f);
        Vector3 targetPos = targetNode.GlobalPosition + new Vector3(0f, 1.0f, 0f);

        // Validación física de trayectoria obligatoria (P0 anti-wallhack)
        bool hasClearTrajectory = false;

        if (_lineOfSightRay != null)
        {
            _lineOfSightRay.GlobalPosition = startPos;
            _lineOfSightRay.TargetPosition = _lineOfSightRay.ToLocal(targetPos);
            _lineOfSightRay.ForceRaycastUpdate();

            if (_lineOfSightRay.IsColliding())
            {
                var collider = _lineOfSightRay.GetCollider() as Node;
                hasClearTrajectory = (collider != null && collider.IsInGroup("Player"));
            }
        }
        else
        {
            // Consulta directa a través del espacio físico si no hay RayCast específico
            var spaceState = _actor.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(startPos, targetPos);
            query.CollisionMask = 1 | 2; // Geometría del mundo y jugador
            var hit = spaceState.IntersectRay(query);

            if (hit.Count > 0 && hit.TryGetValue("collider", out var colObj))
            {
                var collider = colObj.AsGodotObject() as Node;
                hasClearTrajectory = (collider != null && collider.IsInGroup("Player"));
            }
        }

        if (hasClearTrajectory)
        {
            _onAttackDamageApplied?.Invoke(targetId, _attackDamage);
            GD.Print($"[GodotActionExecutor] Disparo con trayectoria libre conectado en {targetId} por {_attackDamage} daño.");
        }
        else
        {
            GD.Print($"[GodotActionExecutor] Disparo bloqueado por oclusión física (muro/cobertura). Daño evitado.");
        }
    }

    public void Cancel(string npcId, string reason)
    {
        _motor.Stop();
        GD.Print($"[ActionExecutor {npcId}] Acción cancelada: {reason}");
    }
}
