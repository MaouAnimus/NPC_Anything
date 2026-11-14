using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Puerto de entrada de información sensorial. El adaptador del motor traduce la física (raycasts, conos, distancias)
/// en estímulos puros antes de entregarlos al núcleo.
/// </summary>
public interface IWorldSensorPort
{
    /// <summary>
    /// Recolecta los estímulos percibidos autorizados para un NPC dado en el instante de simulación actual.
    /// </summary>
    IReadOnlyList<Stimulus> Observe(string npcId, long simulationTimeMs);
}
