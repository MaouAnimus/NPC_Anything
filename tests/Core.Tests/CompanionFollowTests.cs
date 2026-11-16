using NpcAnything.Core.Domain;
using NpcAnything.Core.Rules;
using NpcAnything.Core.Tactics;
using Xunit;

namespace NpcAnything.Core.Tests;

public class CompanionFollowTests
{
    private static NpcContext CreateCompanionContext(
        Vec3 companionPos,
        Vec3 playerPos,
        Dictionary<string, MemoryFact>? memory = null)
    {
        return new NpcContext(
            NpcId: "companion_01",
            Role: NpcRole.Companion,
            Position: companionPos,
            HealthRatio: 1.0f,
            CurrentAction: ActionKind.Hold,
            CurrentActionScore: 0.30f,
            CurrentTimeMs: 1000,
            Memory: memory ?? new Dictionary<string, MemoryFact>(),
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>(),
            KnownAllyPositions: new List<Vec3> { playerPos });
    }

    [Fact]
    public void Companion_WithoutThreat_WhenFarFromPlayer_GeneratesFollow()
    {
        var generator = new ActionCandidateGenerator();

        // Jugador a 10m de distancia (eje Z), compañero en el origen (0, 0, 0)
        var context = CreateCompanionContext(
            companionPos: new Vec3(0, 0, 0),
            playerPos: new Vec3(0, 0, 10));

        var candidates = generator.GenerateCandidates(context);

        var follow = candidates.FirstOrDefault(c => c.Kind == ActionKind.Follow);

        Assert.NotNull(follow);
        Assert.True(follow.Score >= 0.65f);
        Assert.Equal("COMPANION_FOLLOW_ALLY_SAFE_DISTANCE", follow.ReasonCode);
        Assert.True(follow.TargetPosition.HasValue);

        // La posición objetivo debe estar a distancia de seguridad del jugador (aprox 3m del jugador), no pegado a él
        float targetDistToPlayer = follow.TargetPosition.Value.DistanceTo(new Vec3(0, 0, 10));
        Assert.InRange(targetDistToPlayer, 2.5f, 3.5f);
    }

    [Fact]
    public void Companion_WithoutThreat_WhenCloseToPlayer_PrefersStandby_DoesNotObstruct()
    {
        var generator = new ActionCandidateGenerator();

        // Jugador a solo 2.0m de distancia (muy cerca, no debe empujar ni obstruir)
        var context = CreateCompanionContext(
            companionPos: new Vec3(0, 0, 0),
            playerPos: new Vec3(0, 0, 2.0f));

        var candidates = generator.GenerateCandidates(context);

        // No debe generar Follow para no empujar ni cruzarse
        Assert.DoesNotContain(candidates, c => c.Kind == ActionKind.Follow);

        // Debe generar Hold en modo standby defensivo
        var hold = candidates.FirstOrDefault(c => c.Kind == ActionKind.Hold);
        Assert.NotNull(hold);
        Assert.Equal("COMPANION_STANDBY_NEAR_ALLY", hold.ReasonCode);
        Assert.True(hold.Score >= 0.50f);
    }

    [Fact]
    public void FairnessFilter_FollowWithoutTargetPosition_IsFilteredOut()
    {
        var context = CreateCompanionContext(
            companionPos: new Vec3(0, 0, 0),
            playerPos: new Vec3(0, 0, 10));

        var raw = new[]
        {
            new ActionCandidate(ActionKind.Follow, 0.80f, "FOLLOW_NO_POS", TargetPosition: null)
        };

        var filtered = FairnessFilter.ApplyFairnessRules(raw, context);
        Assert.Empty(filtered);
    }
}
