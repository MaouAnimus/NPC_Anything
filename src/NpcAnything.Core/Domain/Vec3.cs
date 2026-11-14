namespace NpcAnything.Core.Domain;

/// <summary>
/// Vector tridimensional inmutable y puro, agnóstico a cualquier motor de juegos.
/// </summary>
public readonly record struct Vec3(float X, float Y, float Z)
{
    public static readonly Vec3 Zero = new(0f, 0f, 0f);
    public static readonly Vec3 UnitX = new(1f, 0f, 0f);
    public static readonly Vec3 UnitY = new(0f, 1f, 0f);
    public static readonly Vec3 UnitZ = new(0f, 0f, 1f);

    public float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

    public float Length() => MathF.Sqrt(LengthSquared());

    public float DistanceTo(Vec3 other) => MathF.Sqrt(DistanceSquaredTo(other));

    public float DistanceSquaredTo(Vec3 other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    public Vec3 DirectionTo(Vec3 other)
    {
        float dist = DistanceTo(other);
        if (dist <= 1e-6f) return Zero;
        return new Vec3((other.X - X) / dist, (other.Y - Y) / dist, (other.Z - Z) / dist);
    }

    public Vec3 Normalized()
    {
        float len = Length();
        if (len <= 1e-6f) return Zero;
        return new Vec3(X / len, Y / len, Z / len);
    }

    public float Dot(Vec3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vec3 operator *(float scalar, Vec3 a) => a * scalar;
    public static Vec3 operator /(Vec3 a, float scalar)
    {
        if (MathF.Abs(scalar) <= 1e-6f) throw excitingDivideByZero();
        return new Vec3(a.X / scalar, a.Y / scalar, a.Z / scalar);

        static DivideByZeroException excitingDivideByZero() => new("No se puede dividir Vec3 por cero o valor infinitesimal.");
    }

    public static Vec3 Lerp(Vec3 a, Vec3 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return a + ((b - a) * t);
    }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
