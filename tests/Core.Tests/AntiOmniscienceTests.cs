using System;
using System.Collections.Generic;
using NpcAnything.Core.Coordination;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Memory;
using NpcAnything.Core.Ports;
using NpcAnything.Core.Rules;
using NpcAnything.Core.Tactics;
using NpcAnything.Core.Utility;
using Xunit;

namespace Core.Tests;

/// <summary>
/// Pruebas rigurosas de anti-omnisciencia y justicia de combate (BK-028, RF-01, RF-02, RNF-09).
/// Demuestra formalmente que ningún NPC accede a coordenadas reales ni verdad de mundo sin percepción autorizada.
/// </summary>
public class AntiOmniscienceTests
{
    private readonly ActionCandidateGenerator _generator = new();
    private readonly UtilitySelector _selector = new();

    [Fact]
    public void EnemyWithoutSensoryStimulus_HasZeroKnowledgeOfPlayer_OnlyPatrolsOrHolds()
    {
        // NPC con memoria vacía (el jugador existe en el mundo en (15, 0, 5) pero no ha sido percibido)
        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Hold,
            CurrentActionScore: 0.30f,
            CurrentTimeMs: 1000,
            Memory: new Dictionary<string, MemoryFact>(), // Memoria estrictamente vacía
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>(),
            PatrolWaypoints: new List<Vec3> { new(10, 0, 0), new(-10, 0, 0) });

        var rawCandidates = _generator.GenerateCandidates(context);
        var filteredCandidates = FairnessFilter.ApplyFairnessRules(rawCandidates, context);
        var decision = _selector.SelectBestAction(filteredCandidates, context);

        // Sin percepción, está terminantemente prohibido generar o ejecutar Attack, Pursue o Flank
        Assert.DoesNotContain(filteredCandidates, c => c.Kind == ActionKind.Attack);
        Assert.DoesNotContain(filteredCandidates, c => c.Kind == ActionKind.Pursue);
        Assert.DoesNotContain(filteredCandidates, c => c.Kind == ActionKind.Flank);

        // La decisión resultante debe ser Patrulla con razón transparente
        Assert.Equal(ActionKind.Patrol, decision.Kind);
        Assert.Equal("WAYPOINT_PATROL", decision.ReasonCode);
    }

    [Fact]
    public void AcousticClue_DoesNotProvideTargetCoordinatesForDirectAttack()
    {
        // Un disparo o paso genera un hecho con incertidumbre espacial y procedencia acústica
        var noisyEstimatedPos = new Vec3(8.5f, 0, 3.2f);
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                SubjectId: "Player",
                EstimatedPosition: noisyEstimatedPos,
                Confidence: 0.65f,
                PrecisionMeters: 4.0f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_acoustic_hearing")
        };

        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.60f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = _generator.GenerateCandidates(context);
        var filteredCandidates = FairnessFilter.ApplyFairnessRules(rawCandidates, context);
        var decision = _selector.SelectBestAction(filteredCandidates, context);

        // 1. El generador nunca crea Attack a partir de sonido
        Assert.DoesNotContain(rawCandidates, c => c.Kind == ActionKind.Attack);

        // 2. Si un actor malicioso inyectara un candidato tramposo de ataque, el filtro de equidad lo purga
        var injectedCheatCandidates = new List<ActionCandidate>(rawCandidates)
        {
            new ActionCandidate(ActionKind.Attack, 0.99f, "ATTACK_FROM_SOUND", TargetId: "Player")
        };
        var securedCandidates = FairnessFilter.ApplyFairnessRules(injectedCheatCandidates, context);
        Assert.DoesNotContain(securedCandidates, c => c.Kind == ActionKind.Attack);

        // 3. La acción válida es investigar la pista sonora
        Assert.Equal(ActionKind.Investigate, decision.Kind);
        Assert.Equal("ACOUSTIC_LEAD_INVESTIGATE_SOUND", decision.ReasonCode);
        Assert.Equal(noisyEstimatedPos, decision.TargetPosition);
    }

    [Fact]
    public void TargetLoss_WhenMemoryTtlExpires_TacticalActionsCeaseAndRevertsToPatrol()
    {
        var memoryStore = new MemoryStore(defaultTtlMs: 3000);

        // Estímulo visual en t = 1000
        memoryStore.ProcessStimuli(new[]
        {
            new Stimulus(
                SubjectId: "Player",
                Kind: StimulusType.Visual,
                EstimatedPosition: new Vec3(6.0f, 0, 0),
                Confidence: 1.0f,
                PrecisionMeters: 0.3f,
                ObservedAtMs: 1000,
                SourceId: "godot_vision_raycast")
        }, currentTimeMs: 1000);

        // 1. En t = 2000 (aún vigente dentro de TTL de 3000ms): El objetivo está activo
        var activeFacts = memoryStore.GetActiveFacts(currentTimeMs: 2000);
        Assert.True(activeFacts.ContainsKey("Player"));
        Assert.False(activeFacts["Player"].IsExpired(2000));

        // 2. En t = 5000 (transcurridos 4000ms > TTL de 3000ms): El recuerdo expira
        var expiredFacts = memoryStore.GetActiveFacts(currentTimeMs: 5000);
        Assert.Empty(expiredFacts);

        // 3. El generador táctico con la memoria expirada vuelve a patrulla/hold sin rastro del jugador
        var contextAt5000 = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Pursue,
            CurrentActionScore: 0.70f,
            CurrentTimeMs: 5000,
            Memory: expiredFacts,
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>(),
            PatrolWaypoints: new List<Vec3> { new(0, 0, 10) });

        var candidates = _generator.GenerateCandidates(contextAt5000);
        var validCandidates = FairnessFilter.ApplyFairnessRules(candidates, contextAt5000);
        var decision = _selector.SelectBestAction(validCandidates, contextAt5000);

        Assert.DoesNotContain(validCandidates, c => c.Kind == ActionKind.Attack);
        Assert.DoesNotContain(validCandidates, c => c.Kind == ActionKind.Pursue);
        Assert.Equal(ActionKind.Patrol, decision.Kind);
    }

    [Fact]
    public void SquadRadio_TransmissionDelayAndRangeIsEnforced_NoInstantaneousOmniscience()
    {
        var bus = new SquadMessageBus();
        long sendTime = 1000;
        long deliveryDelay = 800; // 800ms de retardo
        float maxRange = 25.0f;

        var message = new SquadMessage(
            SenderId: "spotter_npc",
            SquadId: "squad_alpha",
            MessageType: "CONTACT_ESTIMATED",
            TargetPosition: new Vec3(12.0f, 0, 4.0f),
            Confidence: 0.95f,
            SentAtMs: sendTime,
            ExpiresAtMs: sendTime + 4000,
            DeliverAtMs: sendTime + deliveryDelay,
            SenderPosition: new Vec3(0, 0, 0),
            MaxRangeMeters: maxRange);

        bus.PostMessage(message);

        // 1. Inmediatamente en t = 1200ms (antes de DeliverAtMs = 1800ms): Ningún aliado recibe el mensaje
        var earlyIncoming = bus.ReceiveMessages("receiver_npc", "squad_alpha", currentTimeMs: 1200, receiverPosition: new Vec3(10.0f, 0, 0));
        Assert.Empty(earlyIncoming);

        // 2. En t = 1850ms pero fuera de alcance físico (distancia 35m > 25m maxRange): El aliado aislado no recibe nada
        var outOfRangeIncoming = bus.ReceiveMessages("receiver_npc", "squad_alpha", currentTimeMs: 1850, receiverPosition: new Vec3(35.0f, 0, 0));
        Assert.Empty(outOfRangeIncoming);

        // 3. En t = 1850ms y dentro de alcance físico (distancia 10m <= 25m): El aliado recibe la alerta con retardo realista
        var inRangeIncoming = bus.ReceiveMessages("receiver_npc", "squad_alpha", currentTimeMs: 1850, receiverPosition: new Vec3(10.0f, 0, 0));
        Assert.Single(inRangeIncoming);
        Assert.Equal("CONTACT_ESTIMATED", inRangeIncoming[0].MessageType);
    }
}
