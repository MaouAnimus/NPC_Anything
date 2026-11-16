using System.Collections.Concurrent;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.Core.Coordination;

/// <summary>
/// Implementación en memoria del bus de comunicación de escuadrón y reserva de coberturas.
/// </summary>
public class SquadMessageBus : ISquadMessageBus
{
    private record CoverReservation(string CoverId, string NpcId, long ExpiresAtMs);

    private readonly ConcurrentBag<SquadMessage> _messages = new();
    private readonly ConcurrentDictionary<string, CoverReservation> _reservations = new();

    public void PostMessage(SquadMessage message)
    {
        _messages.Add(message);
    }

    public IReadOnlyList<SquadMessage> ReceiveMessages(string npcId, string squadId, long currentTimeMs, Vec3? receiverPosition = null)
    {
        // Retorna mensajes vigentes del escuadrón respetando latencia y límite de alcance
        return _messages
            .Where(m =>
            {
                if (m.SquadId != squadId || m.SenderId == npcId || m.ExpiresAtMs <= currentTimeMs)
                {
                    return false;
                }

                // Control de retardo: si todavía está en tránsito de transmisión, no se entrega aún
                if (m.DeliverAtMs > currentTimeMs)
                {
                    return false;
                }

                // Control de alcance físico/radio (BK-021)
                if (receiverPosition.HasValue && m.SenderPosition.HasValue && m.MaxRangeMeters < float.MaxValue)
                {
                    float dist = receiverPosition.Value.DistanceTo(m.SenderPosition.Value);
                    if (dist > m.MaxRangeMeters)
                    {
                        return false;
                    }
                }

                return true;
            })
            .OrderBy(m => m.SentAtMs)
            .ToList();
    }

    public bool TryReserveCover(string squadId, string coverId, string npcId, long currentTimeMs, long durationMs)
    {
        string key = $"{squadId}_{coverId}";
        long expires = currentTimeMs + durationMs;

        var newReservation = new CoverReservation(coverId, npcId, expires);

        return _reservations.AddOrUpdate(
            key,
            newReservation,
            (_, current) =>
            {
                // Si la reserva actual ya expiró o pertenece al mismo NPC, se permite renovar
                if (current.ExpiresAtMs <= currentTimeMs || current.NpcId == npcId)
                {
                    return newReservation;
                }
                return current; // Ya reservada por otro aliado
            }) == newReservation;
    }

    public void ReleaseCover(string squadId, string coverId, string npcId)
    {
        string key = $"{squadId}_{coverId}";
        if (_reservations.TryGetValue(key, out var res) && res.NpcId == npcId)
        {
            _reservations.TryRemove(key, out _);
        }
    }

    public IReadOnlySet<string> GetReservedCovers(string squadId, long currentTimeMs)
    {
        var active = new HashSet<string>();
        foreach (var (key, res) in _reservations)
        {
            if (key.StartsWith(squadId) && res.ExpiresAtMs > currentTimeMs)
            {
                active.Add(res.CoverId);
            }
        }
        return active;
    }

    public void Clear()
    {
        _reservations.Clear();
        // ConcurrentBag no tiene Clear en todas las versiones, pero podemos drenar o reinicializar si fuera necesario
    }
}
