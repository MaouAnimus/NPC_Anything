using System.Collections.Concurrent;
using NpcAnything.Core.Coordination;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Registro y proveedor de buses de comunicación de escuadrón en Godot.
/// Permite que múltiples NPCs en la misma escena compartan el mismo canal de coordinación táctica.
/// </summary>
public static class GodotSquadService
{
    private static readonly ConcurrentDictionary<string, ISquadMessageBus> _buses = new();

    public static ISquadMessageBus GetOrCreateSquadBus(string squadId)
    {
        return _buses.GetOrAdd(squadId, _ => new SquadMessageBus());
    }

    public static void Reset()
    {
        _buses.Clear();
    }
}
