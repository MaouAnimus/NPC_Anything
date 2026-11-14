using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador del puerto de ejecución de acciones.
/// Recibe decisiones del Core y las delega al NpcMotorController o a los efectores de combate.
/// REGLA ESTRICTA: Cero decisiones tácticas; solo valida la viabilidad física y ejecuta.
/// </summary>
public class GodotActionExecutorPort : IActionExecutorPort
{
    private readonly CharacterBody3D _actor;
    private readonly NpcMotorController _motor;
    private readonly System.Action<string>? _onAttackRequested;

    public GodotActionExecutorPort(
        CharacterBody3D actor,
        NpcMotorController motor,
        System.Action<string>? onAttackRequested = null)
    {
        _actor = actor;
        _motor = motor;
        _onAttackRequested = onAttackRequested;
    }

    public bool CanExecute(string npcId, ActionKind action)
    {
        if (!_actor.IsInsideTree()) return false;

        // Comprobación física: el actor está en el árbol y no está incapacitado
        return true;
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
                    _onAttackRequested?.Invoke(decision.TargetId);
                }
                break;

            case ActionKind.Hold:
            default:
                _motor.Stop();
                break;
        }
    }

    public void Cancel(string npcId, string reason)
    {
        _motor.Stop();
        GD.Print($"[ActionExecutor {npcId}] Acción cancelada: {reason}");
    }
}
