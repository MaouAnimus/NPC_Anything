using Godot;
using NpcAnything.GodotAdapter;

namespace GodotArena.Scripts;

/// <summary>
/// Actor enemigo controlado por NpcAnything.Core a través del adaptador NpcAnything.Godot.
/// </summary>
public partial class CoreEnemy : CharacterBody3D
{
    [Export] public string NpcId { get; set; } = "enemy_core_01";

    public override void _Ready()
    {
        AddToGroup("Enemies");

        // Asegurar que el cerebro y motor estén conectados
        var brain = GetNodeOrNull<NpcBrainController>("NpcBrainController");
        if (brain == null)
        {
            brain = new NpcBrainController();
            brain.Name = "NpcBrainController";
            brain.NpcId = NpcId;
            AddChild(brain);
        }
    }
}
