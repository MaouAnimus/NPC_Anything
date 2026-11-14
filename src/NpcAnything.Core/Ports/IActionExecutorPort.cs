using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Puerto de salida para la ejecución de acciones en el motor de juego (navegación, animaciones, disparos).
/// </summary>
public interface IActionExecutorPort
{
    /// <summary>
    /// Consulta si el motor puede ejecutar la acción en el estado físico actual del actor.
    /// </summary>
    bool CanExecute(string npcId, ActionKind action);

    /// <summary>
    /// Solicita al motor la ejecución de una decisión tomada por el núcleo.
    /// </summary>
    void Request(string npcId, ActionDecision decision);

    /// <summary>
    /// Cancela una acción activa con un motivo auditable.
    /// </summary>
    void Cancel(string npcId, string reason);
}
