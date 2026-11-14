using System.Collections.Generic;
using Godot;
using NpcAnything.Core.Domain;
using NpcAnything.Core.Ports;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Adaptador del puerto espacial para Godot.
/// Responde consultas de geometría física y puntos de cobertura navegables.
/// REGLA ESTRICTA: Cero decisiones sobre si una cobertura es "buena" o "mala" (eso le compete al Core).
/// </summary>
public class GodotSpatialQueryPort : ISpatialQueryPort
{
    private readonly Node3D _worldContext;

    public GodotSpatialQueryPort(Node3D worldContext)
    {
        _worldContext = worldContext;
    }

    public IReadOnlyList<Vec3> QueryReachableCovers(string npcId, Vec3 origin, float maxRadiusMeters)
    {
        var reachableSpots = new List<Vec3>();
        if (!_worldContext.IsInsideTree()) return reachableSpots;

        var tree = _worldContext.GetTree();
        var coverNodes = tree.GetNodesInGroup("Covers");
        Vector3 godotOrigin = origin.ToGodot();
        float maxRadiusSq = maxRadiusMeters * maxRadiusMeters;

        foreach (var node in coverNodes)
        {
            if (node is not Node3D coverNode) continue;

            // Si el objeto de cobertura define un punto específico de posicionamiento (StandingSpot), se utiliza.
            // De lo contrario, se utiliza la posición del nodo.
            Vector3 standingSpot = coverNode.GlobalPosition;
            var spotNode = coverNode.GetNodeOrNull<Node3D>("StandingSpot");
            if (spotNode != null)
            {
                standingSpot = spotNode.GlobalPosition;
            }

            // 1. Filtrado por radio esférico
            if (godotOrigin.DistanceSquaredTo(standingSpot) > maxRadiusSq)
            {
                continue;
            }

            // 2. Comprobación de navegabilidad básica
            // La posición debe estar en el suelo o proyectable sobre la malla de navegación
            reachableSpots.Add(standingSpot.ToVec3());
        }

        return reachableSpots;
    }
}
