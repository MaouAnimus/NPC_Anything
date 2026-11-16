using NpcAnything.Core.Coordination;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;
using Xunit;

namespace NpcAnything.Core.Tests;

public class SquadCoordinationTests
{
    [Fact]
    public void ReceiveMessages_RespectsTransmissionDelay()
    {
        var bus = new SquadMessageBus();
        long nowMs = 1000;
        long deliverAtMs = 1600; // Retardo de 600ms

        var message = new SquadMessage(
            SenderId: "enemy_01",
            SquadId: "squad_alpha",
            MessageType: "CONTACT_ESTIMATED",
            TargetPosition: new Vec3(10, 0, 10),
            Confidence: 0.95f,
            SentAtMs: nowMs,
            DeliverAtMs: deliverAtMs,
            ExpiresAtMs: 5000);

        bus.PostMessage(message);

        // A t=1200ms (en tránsito): no debe recibirse aún
        var earlyMessages = bus.ReceiveMessages("enemy_02", "squad_alpha", currentTimeMs: 1200);
        Assert.Empty(earlyMessages);

        // A t=1600ms (tiempo cumplido): se recibe
        var deliveredMessages = bus.ReceiveMessages("enemy_02", "squad_alpha", currentTimeMs: 1600);
        Assert.Single(deliveredMessages);
        Assert.Equal("CONTACT_ESTIMATED", deliveredMessages[0].MessageType);
    }

    [Fact]
    public void ReceiveMessages_RespectsPhysicalMaxRange()
    {
        var bus = new SquadMessageBus();
        long nowMs = 1000;

        var message = new SquadMessage(
            SenderId: "enemy_01",
            SquadId: "squad_alpha",
            MessageType: "CONTACT_ESTIMATED",
            TargetPosition: new Vec3(10, 0, 10),
            Confidence: 0.90f,
            SentAtMs: nowMs,
            DeliverAtMs: nowMs,
            ExpiresAtMs: 5000,
            SenderPosition: new Vec3(0, 0, 0),
            MaxRangeMeters: 25.0f); // Alcance de 25m

        bus.PostMessage(message);

        // Aliado cercano a 10m: recibe el mensaje
        var closeAlly = bus.ReceiveMessages("enemy_02", "squad_alpha", nowMs, receiverPosition: new Vec3(10, 0, 0));
        Assert.Single(closeAlly);

        // Aliado lejano a 35m: fuera de alcance, no recibe
        var farAlly = bus.ReceiveMessages("enemy_03", "squad_alpha", nowMs, receiverPosition: new Vec3(35, 0, 0));
        Assert.Empty(farAlly);
    }

    [Fact]
    public void TryReserveCover_PreventsDualReservation_ByAllies()
    {
        var bus = new SquadMessageBus();
        long nowMs = 1000;
        string squadId = "squad_alpha";
        string coverId = "cover_spot_01";

        // Primer aliado reserva la cobertura
        bool firstReserved = bus.TryReserveCover(squadId, coverId, "enemy_01", nowMs, durationMs: 4000);
        Assert.True(firstReserved);

        // Segundo aliado intenta reservar el mismo punto al mismo tiempo: debe ser rechazado
        bool secondReserved = bus.TryReserveCover(squadId, coverId, "enemy_02", nowMs, durationMs: 4000);
        Assert.False(secondReserved);

        // Las reservas activas contienen el ID de la cobertura
        var activeCovers = bus.GetReservedCovers(squadId, nowMs);
        Assert.Contains(coverId, activeCovers);
    }

    [Fact]
    public void ReleaseCover_AllowsAnotherAllyToReserve()
    {
        var bus = new SquadMessageBus();
        long nowMs = 1000;
        string squadId = "squad_alpha";
        string coverId = "cover_spot_02";

        bus.TryReserveCover(squadId, coverId, "enemy_01", nowMs, durationMs: 4000);

        // El primer aliado libera la cobertura
        bus.ReleaseCover(squadId, coverId, "enemy_01");

        // Ahora el segundo aliado sí puede reservarla
        bool secondReserved = bus.TryReserveCover(squadId, coverId, "enemy_02", nowMs, durationMs: 4000);
        Assert.True(secondReserved);
    }
}
