using NpcAnything.Core.Domain;
using NpcAnything.Core.Utility;
using Xunit;

namespace NpcAnything.Core.Tests;

public class UtilitySelectorTests
{
    private static NpcContext CreateDummyContext(ActionKind currentAction, float currentScore)
    {
        return new NpcContext(
            NpcId: "npc_01",
            Role: NpcRole.Assault,
            Position: new Vec3(0, 0, 0),
            HealthRatio: 1.0f,
            CurrentAction: currentAction,
            CurrentActionScore: currentScore,
            CurrentTimeMs: 1000,
            Memory: new Dictionary<string, MemoryFact>(),
            ReachableCoverPoints: new List<Vec3>(),
            ReservedSquadCovers: new HashSet<string>());
    }

    [Fact]
    public void SelectBestAction_EmptyCandidates_ReturnsFallbackHold()
    {
        var selector = new UtilitySelector();
        var context = CreateDummyContext(ActionKind.Patrol, 0.5f);

        var decision = selector.SelectBestAction(Array.Empty<ActionCandidate>(), context);

        Assert.Equal(ActionKind.Hold, decision.Kind);
        Assert.Equal("FALLBACK_NO_CANDIDATES", decision.ReasonCode);
    }

    [Fact]
    public void SelectBestAction_BelowThreshold_ReturnsFallbackHold()
    {
        var selector = new UtilitySelector(minimumViableScore: 0.20f);
        var context = CreateDummyContext(ActionKind.Patrol, 0.5f);

        var candidates = new[]
        {
            new ActionCandidate(ActionKind.Patrol, 0.10f, "PATROL_LOW")
        };

        var decision = selector.SelectBestAction(candidates, context);

        Assert.Equal(ActionKind.Hold, decision.Kind);
        Assert.Equal("FALLBACK_ALL_SCORES_BELOW_THRESHOLD", decision.ReasonCode);
    }

    [Fact]
    public void SelectBestAction_Hysteresis_RetainsCurrentActionWhenDifferenceIsSmall()
    {
        var selector = new UtilitySelector(hysteresisMargin: 0.15f);
        var context = CreateDummyContext(ActionKind.Patrol, 0.60f);

        // TakeCover supera a Patrol solo por 0.05 (menor que el margen 0.15)
        var candidates = new[]
        {
            new ActionCandidate(ActionKind.Patrol, 0.60f, "PATROL_OK"),
            new ActionCandidate(ActionKind.TakeCover, 0.65f, "COVER_NEARBY")
        };

        var decision = selector.SelectBestAction(candidates, context);

        Assert.Equal(ActionKind.Patrol, decision.Kind);
        Assert.Contains("RETAINED_BY_HYSTERESIS", decision.ReasonCode);
    }

    [Fact]
    public void SelectBestAction_Hysteresis_SwitchesWhenDifferenceExceedsMargin()
    {
        var selector = new UtilitySelector(hysteresisMargin: 0.15f);
        var context = CreateDummyContext(ActionKind.Patrol, 0.50f);

        // TakeCover supera a Patrol por 0.30 (mayor que el margen 0.15)
        var candidates = new[]
        {
            new ActionCandidate(ActionKind.Patrol, 0.50f, "PATROL_OK"),
            new ActionCandidate(ActionKind.TakeCover, 0.80f, "COVER_URGENT")
        };

        var decision = selector.SelectBestAction(candidates, context);

        Assert.Equal(ActionKind.TakeCover, decision.Kind);
        Assert.Equal("COVER_URGENT", decision.ReasonCode);
    }
}
