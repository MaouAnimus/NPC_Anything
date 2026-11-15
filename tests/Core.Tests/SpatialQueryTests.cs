using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;
using Xunit;

namespace NpcAnything.Core.Tests;

public class SpatialQueryTests
{
    /// <summary>
    /// Mock con soporte para topología de navegación, obstáculos con rodeos y zonas desconectadas.
    /// Simula fielmente el comportamiento de NavigationServer3D.MapGetPath() bajo el principio fail-closed.
    /// </summary>
    private class MockNavMeshSpatialQueryPort : ISpatialQueryPort
    {
        private readonly List<Vec3> _allCoverStandingSpots;
        private readonly HashSet<Vec3> _disconnectedSpots;
        private readonly Dictionary<Vec3, float> _pathLengthOverrides;
        private readonly bool _isNavigationAvailable;

        public MockNavMeshSpatialQueryPort(
            IEnumerable<Vec3> coverSpots,
            IEnumerable<Vec3>? disconnectedSpots = null,
            Dictionary<Vec3, float>? pathLengthOverrides = null,
            bool isNavigationAvailable = true)
        {
            _allCoverStandingSpots = coverSpots.ToList();
            _disconnectedSpots = disconnectedSpots != null ? new HashSet<Vec3>(disconnectedSpots) : new();
            _pathLengthOverrides = pathLengthOverrides ?? new();
            _isNavigationAvailable = isNavigationAvailable;
        }

        public IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters)
        {
            // Fail closed: si el mapa de navegación no está disponible, retorna lista vacía
            if (!_isNavigationAvailable)
            {
                return Array.Empty<Vec3>();
            }

            var reachable = new List<Vec3>();
            float maxRadiusSq = maxRadiusMeters * maxRadiusMeters;

            foreach (var spot in _allCoverStandingSpots)
            {
                // 1. Filtro rápido euclidiano
                if (origin.DistanceSquaredTo(spot) > maxRadiusSq) continue;

                // 2. Zona desconectada (sin ruta en NavMesh)
                if (_disconnectedSpots.Contains(spot)) continue;

                // 3. Longitud real de ruta (con posibles rodeos por muros)
                float pathLength = _pathLengthOverrides.TryGetValue(spot, out var length)
                    ? length
                    : origin.DistanceTo(spot);

                if (pathLength <= maxRadiusMeters * 1.6f)
                {
                    reachable.Add(spot);
                }
            }

            return reachable;
        }

        public bool IsPathReachable(Vec3 origin, Vec3 destination, float maxPathLength)
        {
            // Fail closed: nunca declarar alcanzable sin navegación válida
            if (!_isNavigationAvailable) return false;

            if (_disconnectedSpots.Contains(destination)) return false;

            float actualPathLength = _pathLengthOverrides.TryGetValue(destination, out var length)
                ? length
                : origin.DistanceTo(destination);

            return actualPathLength <= maxPathLength;
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

        var port = new MockNavMeshSpatialQueryPort(coverPoints);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 5.0f);

        Assert.Equal(2, reachable.Count);
        Assert.Contains(new Vec3(2f, 0f, 0f), reachable);
        Assert.Contains(new Vec3(0f, 0f, 4f), reachable);
        Assert.DoesNotContain(new Vec3(10f, 0f, 0f), reachable);
    }

    [Fact]
    public void QueryReachableCovers_ObstacleForcesExcessivePath_ExcludesCover()
    {
        var nearCoverBehindWall = new Vec3(3f, 0f, 0f); // Euclidiana = 3m
        var coverPoints = new[] { nearCoverBehindWall };

        // El muro obliga a rodear con una ruta real de 12 metros (> 5m * 1.6 = 8m)
        var pathOverrides = new Dictionary<Vec3, float>
        {
            [nearCoverBehindWall] = 12.0f
        };

        var port = new MockNavMeshSpatialQueryPort(coverPoints, pathLengthOverrides: pathOverrides);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 5.0f);

        // A pesar de estar a 3m en línea recta, el obstáculo la hace inalcanzable tácticamente
        Assert.DoesNotContain(nearCoverBehindWall, reachable);
    }

    [Fact]
    public void QueryReachableCovers_DisconnectedNavigationIsland_ExcludesCover()
    {
        var islandCover = new Vec3(2f, 0f, 0f);
        var coverPoints = new[] { islandCover };
        var disconnected = new[] { islandCover };

        var port = new MockNavMeshSpatialQueryPort(coverPoints, disconnectedSpots: disconnected);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 5.0f);

        Assert.Empty(reachable);
    }

    [Fact]
    public void QueryReachableCovers_FailClosed_WhenNavigationNotAvailable()
    {
        var coverPoints = new[] { new Vec3(2f, 0f, 0f), new Vec3(1f, 0f, 1f) };
        var port = new MockNavMeshSpatialQueryPort(coverPoints, isNavigationAvailable: false);
        var origin = new Vec3(0f, 0f, 0f);

        var reachable = port.QueryReachableCovers("npc_test", origin, maxRadiusMeters: 10.0f);

        // Fail-closed estricto: retorna lista vacía
        Assert.Empty(reachable);
    }

    [Fact]
    public void IsPathReachable_VerifiesDistanceThreshold()
    {
        var port = new MockNavMeshSpatialQueryPort(Array.Empty<Vec3>());
        var origin = new Vec3(0f, 0f, 0f);
        var inRange = new Vec3(3f, 4f, 0f); // Distancia 5
        var outOfRange = new Vec3(10f, 10f, 0f);

        Assert.True(port.IsPathReachable(origin, inRange, maxPathLength: 6.0f));
        Assert.False(port.IsPathReachable(origin, outOfRange, maxPathLength: 6.0f));
    }

    [Fact]
    public void IsPathReachable_FailClosed_WhenNavigationDisabled_ReturnsFalse()
    {
        var port = new MockNavMeshSpatialQueryPort(Array.Empty<Vec3>(), isNavigationAvailable: false);
        var origin = new Vec3(0f, 0f, 0f);
        var target = new Vec3(1f, 0f, 0f); // Muy cerca

        // Fail-closed: jamás retorna true si la navegación está caída
        Assert.False(port.IsPathReachable(origin, target, maxPathLength: 10.0f));
    }
}
