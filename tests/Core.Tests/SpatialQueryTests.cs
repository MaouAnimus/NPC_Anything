using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;
using Xunit;

namespace NpcAnything.Core.Tests;

public class SpatialQueryTests
{
    private class MockSpatialQueryPort : ISpatialQueryPort
    {
        private readonly List<Vec3> _allCoverStandingSpots;

        public MockSpatialQueryPort(IEnumerable<Vec3> coverSpots)
        {
            _allCoverStandingSpots = coverSpots.ToList();
        }

        public IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters)
        {
            float maxRadiusSq = maxRadiusMeters * maxRadiusMeters;
            return _allCoverStandingSpots
                .Where(spot => origin.DistanceSquaredTo(spot) <= maxRadiusSq)
                .ToList();
        }
    }

    [Fact]
    public void QueryReachableCovers_ReturnsOnlyPointsWithinRadius()
    {
        var coverPoints = new[]
        {
            new Vec3(2f, 0f, 0f),   // Distancia 2m (dentro de 5m)
            new Vec3(0f, 0f, 4f),   // Distancia 4m (dentro de 5m)
            new Vec3(10f, 0f, 0f),  // Distancia 10m (fuera de 5m)
            new Vec3(0f, 0f, -6f)   // Distancia 6m (fuera de 5m)
        };

        var port = new MockSpatialQueryPort(coverPoints);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 5.0f);

        Assert.Equal(2, reachable.Count);
        Assert.Contains(new Vec3(2f, 0f, 0f), reachable);
        Assert.Contains(new Vec3(0f, 0f, 4f), reachable);
        Assert.DoesNotContain(new Vec3(10f, 0f, 0f), reachable);
    }

    [Fact]
    public void QueryReachableCovers_NoCoversInRange_ReturnsEmptyList()
    {
        var coverPoints = new[]
        {
            new Vec3(20f, 0f, 20f),
            new Vec3(-30f, 0f, 0f)
        };

        var port = new MockSpatialQueryPort(coverPoints);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 10.0f);

        Assert.Empty(reachable);
    }
}
