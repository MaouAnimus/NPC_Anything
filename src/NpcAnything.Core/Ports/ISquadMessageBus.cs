using NpcAnything.Core.Domain;

namespace NpcAnything.Core.Ports;

/// <summary>
/// Mensaje estructurado de coordinación entre miembros de un mismo escuadrón.
/// </summary>
public record SquadMessage(
    string SenderId,
    string SquadId,
    string MessageType,
    Vec3? TargetPosition,
    float Confidence,
    long SentAtMs,
    long ExpiresAtMs);

/// <summary>
/// Puerto de comunicación y sincronización táctica entre NPCs aliados.
/// </summary>
public interface ISquadMessageBus
{
    /// <summary>
    /// Difunde un mensaje a los aliados del escuadrón respetando latencia y expiración.
    /// </summary>
    void PostMessage(SquadMessage message);

    /// <summary>
    /// Obtiene y drena los mensajes vigentes pendientes para el NPC receptor.
    /// </summary>
    IReadOnlyList<SquadMessage> ReceiveMessages(string npcId, string squadId, long currentTimeMs);

    /// <summary>
    /// Intenta reservar un punto de cobertura para evitar que múltiples agentes intenten ocuparlo simultáneamente.
    /// </summary>
    bool TryReserveCover(string squadId, string coverId, string npcId, long currentTimeMs, long durationMs);

    /// <summary>
    /// Libera una cobertura reservada previamente por el agente.
    /// </summary>
    void ReleaseCover(string squadId, string coverId, string npcId);

    /// <summary>
    /// Obtiene las coberturas actualmente reservadas en el escuadrón.
    /// </summary>
    IReadOnlySet<string> GetReservedCovers(string squadId, long currentTimeMs);
}
