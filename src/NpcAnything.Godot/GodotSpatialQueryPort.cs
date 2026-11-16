using System.Collections.Generic;
using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador del puerto espacial para Godot.
/// Valida la conectividad física de rutas en la malla de navegación real utilizando NavigationServer3D.MapGetPath().
/// REGLA ESTRICTA: Cero decisiones sobre conveniencia táctica.
/// </summary>
public class GodotSpatialQueryPort : ISpatialQueryPort
{
    private readonly Node3D _worldContext;

    public GodotSpatialQueryPort(Node3D worldContext)
    {
        _worldContext = worldContext;
    }

    public bool IsNavigationReady
    {
        get
        {
            if (!_worldContext.IsInsideTree()) return false;

            Rid navMap = _worldContext.GetWorld3D()?.NavigationMap ?? default;
            if (navMap == default || !navMap.IsValid || NavigationServer3D.MapGetIterationId(navMap) == 0)
            {
                return false;
            }

            var env = _worldContext.GetTree().Root.FindChild("ArenaEnvironment", recursive: true, owned: false);
            if (env != null)
            {
                var val = env.Get("NavigationReady");
                if (val.VariantType == Variant.Type.Bool && !val.AsBool())
                {
                    return false;
                }
            }

            return true;
        }
    }

    public IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters)
    {
        var reachableSpots = new List<Vec3>();
        if (!_worldContext.IsInsideTree()) return reachableSpots;

        var tree = _worldContext.GetTree();
        var coverNodes = tree.GetNodesInGroup("Covers");
        Vector3 godotOrigin = origin.ToGodot();
        float maxRadiusSq = maxRadiusMeters * maxRadiusMeters;

        Rid navMap = _worldContext.GetWorld3D()?.NavigationMap ?? default;
        if (navMap == default || !navMap.IsValid || NavigationServer3D.MapGetIterationId(navMap) == 0)
        {
            GD.PushWarning($"[GodotSpatialQueryPort] NavigationMap no está sincronizado para {npcId}. Fail-closed: 0 coberturas alcanzables.");
            return reachableSpots;
        }

        foreach (var node in coverNodes)
        {
            if (node is not Node3D coverNode) continue;

            Vector3 standingSpot = coverNode.GlobalPosition;
            var spotNode = coverNode.GetNodeOrNull<Node3D>("StandingSpot");
            if (spotNode != null)
            {
                standingSpot = spotNode.GlobalPosition;
            }

            // 1. Filtro rápido de distancia esférica en línea recta
            if (godotOrigin.DistanceSquaredTo(standingSpot) > maxRadiusSq)
            {
                continue;
            }

            // 2. Validación obligatoria de ruta navegable real (P0 - Fail Closed)
            Vector3[] path = NavigationServer3D.MapGetPath(navMap, godotOrigin, standingSpot, optimize: true);

            // Si no hay ruta o tiene menos de 2 puntos, es inalcanzable
            if (path.Length < 2)
            {
                continue;
            }

            // Verificar que el final de la ruta realmente llega a la cobertura (no se detiene ante un abismo o muro)
            Vector3 finalPathPoint = path[^1];
            if (finalPathPoint.DistanceTo(standingSpot) > 1.2f)
            {
                continue;
            }

            // Calcular longitud real acumulada del camino
            float totalPathLength = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                totalPathLength += path[i].DistanceTo(path[i + 1]);
            }

            if (totalPathLength > maxRadiusMeters * 1.6f)
            {
                continue; // Ruta excesivamente larga u obstruida
            }

            reachableSpots.Add(standingSpot.ToVec3());
        }

        return reachableSpots;
    }

    public bool IsPathReachable(Vec3 origin, Vec3 destination, float maxPathLength)
    {
        if (!_worldContext.IsInsideTree()) return false;

        Rid navMap = _worldContext.GetWorld3D()?.NavigationMap ?? default;
        if (navMap == default || !navMap.IsValid || NavigationServer3D.MapGetIterationId(navMap) == 0)
        {
            GD.PushWarning("[GodotSpatialQueryPort] NavigationMap no disponible o sin sincronizar. Fail-closed: ruta no alcanzable.");
            return false;
        }

        Vector3 start = origin.ToGodot();
        Vector3 end = destination.ToGodot();

        Vector3[] path = NavigationServer3D.MapGetPath(navMap, start, end, optimize: true);
        if (path.Length < 2) return false;
        if (path[^1].DistanceTo(end) > 1.2f) return false;

        float totalPathLength = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            totalPathLength += path[i].DistanceTo(path[i + 1]);
        }

        return totalPathLength <= maxPathLength;
    }
}
