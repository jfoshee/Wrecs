using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class CircleConvexPolygonContactTests
{
    [Fact(DisplayName = "Circle can move away after contacting an axis-aligned polygon edge")]
    public void TrySweepIntersection_AxisAlignedEdgeContact_CanMoveAway()
    {
        var wallCoordinates = new[] { 10f, 10.3f, 0f, -10.3f, -1000f, 1000f };
        var radii = new[] { 0.1f, 0.3f, 1f, 2f, 2.2f, 12f };
        var approaches = new[] { 0.1f, 0.3f, 1f, 7.7f, 20f };

        foreach (var wallX in wallCoordinates)
        foreach (var radius in radii)
        foreach (var approach in approaches)
        {
            var polygon = CreatePolygon(wallX, 0f);
            var start = new Circle(new Vector2(wallX - radius - approach, 0f), radius);
            var destination = start with
            {
                Center = start.Center + new Vector2(approach + radius, 0f)
            };

            start.TrySweepIntersection(destination, polygon, out var contact).Should().BeTrue();
            var touching = start with
            {
                Center = start.Center + contact.GetAllowedMovement(
                    destination.Center - start.Center)
            };
            var movingAway = touching with { Center = touching.Center + contact.Normal };
            var tangent = new Vector2(-contact.Normal.Y, contact.Normal.X);
            var movingTangent = touching with { Center = touching.Center + tangent };

            touching.TrySweepIntersection(movingAway, polygon, out _).Should().BeFalse(
                $"a radius {radius} circle at x={wallX} should separate after contact");
            touching.TrySweepIntersection(movingTangent, polygon, out _).Should().BeFalse(
                $"a radius {radius} circle at x={wallX} should slide after contact");
        }
    }

    [Fact(DisplayName = "Circle can move away after contacting a nearly axis-aligned polygon edge")]
    public void TrySweepIntersection_NearlyAxisAlignedEdgeContact_CanMoveAway()
    {
        var wallCoordinates = new[] { 10f, 10.3f, 0f, -10.3f, -1000f, 1000f };
        var radii = new[] { 0.1f, 0.3f, 1f, 2f, 2.2f, 12f };
        var approaches = new[] { 0.1f, 0.3f, 1f, 7.7f, 20f };
        var tilts = new[] { 0.000001f, 0.00001f, 0.0001f, 0.001f, 0.01f, 0.1f };

        foreach (var wallX in wallCoordinates)
        foreach (var radius in radii)
        foreach (var approach in approaches)
        foreach (var tilt in tilts)
        {
            var polygon = CreatePolygon(wallX, tilt);
            var facePoint = new Vector2(wallX + tilt / 2f, 0f);
            var normal = polygon.EdgeNormals[3];
            var start = new Circle(facePoint + normal * (radius + approach), radius);
            var destination = start with
            {
                Center = start.Center - normal * (approach + radius)
            };

            start.TrySweepIntersection(destination, polygon, out var contact).Should().BeTrue();
            var touching = start with
            {
                Center = start.Center + contact.GetAllowedMovement(
                    destination.Center - start.Center)
            };
            var movingAway = touching with { Center = touching.Center + contact.Normal };
            var tangent = new Vector2(-contact.Normal.Y, contact.Normal.X);
            var movingTangent = touching with { Center = touching.Center + tangent };

            touching.TrySweepIntersection(movingAway, polygon, out _).Should().BeFalse(
                $"a radius {radius} circle at x={wallX} with tilt {tilt} should separate after contact");
            touching.TrySweepIntersection(movingTangent, polygon, out _).Should().BeFalse(
                $"a radius {radius} circle at x={wallX} with tilt {tilt} should slide after contact");
        }
    }

    private static ConvexPolygon CreatePolygon(float leftX, float tilt) =>
        new([
            new Vector2(leftX, -100f),
            new Vector2(leftX + 20f, -100f),
            new Vector2(leftX + 20f, 100f),
            new Vector2(leftX + tilt, 100f)
        ]);
}
