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
            Memory: new Dictionary<string, MemoryFact>(), // Memoria vacía del jugador
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.Attack, 0.95f, "ATTACK_CHEAT", TargetId: "player")
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Bloqueado: no se permite ataque omnisciente
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
            ReservedSquadCovers: new HashSet<string> { coverKey }); // Ya reservada por un aliado

        var rawCandidates = new[]
        {
            new ActionCandidate(ActionKind.TakeCover, 0.85f, "COVER_CANDIDATE", TargetPosition: reservedCoverPos)
        };

        var filtered = FairnessFilter.ApplyFairnessRules(rawCandidates, context);

        Assert.Empty(filtered); // Descartada: evita que dos aliados vayan al mismo punto
    }
}
