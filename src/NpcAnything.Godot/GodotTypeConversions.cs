using Godot;
using NpcAnything.Core.Domain;

namespace NpcAnything.GodotAdapter;

/// <summary>
/// Métodos de extensión para conversión bidireccional entre los vectores de Godot y los tipos puros del Core.
/// </summary>
public static class GodotTypeConversions
{
    public static Vec3 ToVec3(this Vector3 v) => new(v.X, v.Y, v.Z);

    public static Vector3 ToGodot(this Vec3 v) => new(v.X, v.Y, v.Z);
}
