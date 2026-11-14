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

        public bool IsPathReachable(Vec3 origin, Vec3 destination, float maxPathLength)
        {
            return origin.DistanceTo(destination) <= maxPathLength;
        }
    }

    [Fact]
    public void QueryReachableCovers_ReturnsOnlyPointsWithinRadius()
    {
        var coverPoints = new[]
        {
            new Vec3(2f, 0f, 0f),
            new Vec3(0f, 0f, 4f),
            new Vec3(10f, 0f, 0f),
            new Vec3(0f, 0f, -6f)
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
    public void IsPathReachable_VerifiesDistanceThreshold()
    {
        var port = new MockSpatialQueryPort(Array.Empty<Vec3>());
        var origin = new Vec3(0f, 0f, 0f);
        var inRange = new Vec3(3f, 4f, 0f); // Distancia 5
        var outOfRange = new Vec3(10f, 10f, 0f);

        Assert.True(port.IsPathReachable(origin, inRange, maxPathLength: 6.0f));
        Assert.False(port.IsPathReachable(origin, outOfRange, maxPathLength: 6.0f));
    }
}
