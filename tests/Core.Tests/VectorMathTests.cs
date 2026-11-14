using NpcAnything.Core.Domain;
using Xunit;

namespace NpcAnything.Core.Tests;

public class VectorMathTests
{
    [Fact]
    public void Vec3_DistanceAndMagnitude_CalculateCorrectly()
    {
        var v1 = new Vec3(0, 0, 0);
        var v2 = new Vec3(3, 4, 0);

        Assert.Equal(5f, v1.DistanceTo(v2), 3);
        Assert.Equal(5f, v2.Length(), 3);
        Assert.Equal(25f, v2.LengthSquared(), 3);
    }

    [Fact]
    public void Vec3_Normalized_ProducesUnitLength()
    {
        var v = new Vec3(10, 0, 0);
        var normalized = v.Normalized();

        Assert.Equal(1f, normalized.Length(), 3);
        Assert.Equal(new Vec3(1, 0, 0), normalized);
    }

    [Fact]
    public void Vec3_DirectionTo_PointsTowardsTarget()
    {
        var origin = new Vec3(0, 0, 0);
        var target = new Vec3(0, 10, 0);

        var dir = origin.DirectionTo(target);

        Assert.Equal(new Vec3(0, 1, 0), dir);
    }

    [Fact]
    public void Vec3_DivisionByZero_ThrowsException()
    {
        var v = new Vec3(1, 2, 3);
        Assert.Throws<DivideByZeroException>(() => v / 0f);
    }

    [Fact]
    public void Vec3_Lerp_InterpolatesProperly()
    {
        var a = new Vec3(0, 0, 0);
        var b = new Vec3(10, 20, 30);

        var mid = Vec3.Lerp(a, b, 0.5f);

        Assert.Equal(5f, mid.X, 3);
        Assert.Equal(10f, mid.Y, 3);
        Assert.Equal(15f, mid.Z, 3);
    }
}
