using NpcAnything.Core.Domain;
using NpcAnything.Core.Rules;
using Xunit;

namespace NpcAnything.Core.Tests;

public class FairnessFilterTests
{
    [Fact]
    public void ApplyFairnessRules_AttackWithoutObservation_IsFilteredOut()
    {
        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.5f,
            CurrentTimeMs: 1000,
            Memory: new Dictionary<string, MemoryFact>(), // Memoria vacía
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.Attack, 0.95f, "ATTACK_CHEAT", TargetId: "Player")
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Bloqueado: no se permite ataque omnisciente
    }

    [Fact]
    public void ApplyFairnessRules_AttackWithAcousticProvenance_IsFilteredOut()
    {
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(5, 0, 0),
                Confidence: 0.85f,
                PrecisionMeters: 2.0f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_acoustic_hearing") // Procedencia auditiva
        };

        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.5f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.Attack, 0.90f, "ATTACK_FROM_SOUND", TargetId: "Player")
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Bloqueado: El sonido NO habilita daño directo a través de muros
    }

    [Fact]
    public void ApplyFairnessRules_TakeCover_FilteredIfCoverIsReservedBySquad()
    {
        var reservedCoverPos = new Vec3(10.0f, 0f, 5.0f);
        string coverKey = $"{reservedCoverPos.X:F1}_{reservedCoverPos.Z:F1}";

        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Flanker,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 0.8f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.4f,
            CurrentTimeMs: 1000,
            Memory: new Dictionary<string, MemoryFact>(),
            ReachableCoverPoints: new[] { reservedCoverPos },
            ReservedSquadCovers: new HashSet<string> { coverKey });

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.TakeCover, 0.85f, "COVER_CANDIDATE", TargetPosition: reservedCoverPos)
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Descartada: evita que dos aliados vayan al mismo punto
    }

    [Fact]
    public void ApplyFairnessRules_PursueWithoutVisualObservation_IsFilteredOut()
    {
        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.5f,
            CurrentTimeMs: 1000,
            Memory: new Dictionary<string, MemoryFact>(), // Sin memoria visual
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.Pursue, 0.85f, "PURSUE_NO_VISION", TargetId: "Player", TargetPosition: new Vec3(15, 0, 0))
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Bloqueado: No se permite persecución sin visión activa
    }

    [Fact]
    public void ApplyFairnessRules_PursueWithValidVisualObservation_IsRetained()
    {
        var memory = new Dictionary<string, MemoryFact>
        {
            ["Player"] = new MemoryFact(
                "Player",
                new Vec3(15, 0, 0),
                Confidence: 0.90f,
                PrecisionMeters: 0.3f,
                LastUpdatedMs: 1000,
                ExpiresAtMs: 4000,
                Provenance: "godot_vision_raycast")
        };

        var context = new NpcContext(
            NpcId: "enemy_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Patrol,
            CurrentActionScore: 0.5f,
            CurrentTimeMs: 1000,
            Memory: memory,
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.Pursue, 0.85f, "TARGET_IN_SIGHT_CLOSE_DISTANCE", TargetId: "Player", TargetPosition: new Vec3(15, 0, 0))
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Single(filtered);
        Assert.Equal(ActionKind.Pursue, filtered[0].Kind);
    }
}
