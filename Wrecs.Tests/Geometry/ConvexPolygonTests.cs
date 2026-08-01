#pragma warning disable CA1806 // Do not ignore method results
#pragma warning disable xUnit1046 // Avoid using TheoryDataRow arguments that are not serializable

using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

public class ConvexPolygonTests
{
    public static IEnumerable<TheoryDataRow<Vector2[]>> TooFewVerticesCases()
    {
        yield return new([]);
        yield return new([new Vector2(0f, 0f)]);
        yield return new([new Vector2(0f, 0f), new Vector2(1f, 0f)]);
    }

    [Theory(DisplayName = "0, 1, and 2 vertices throw")]
    [MemberData(nameof(TooFewVerticesCases))]
    public void Constructor_TooFewVertices_Throws(Vector2[] vertices)
    {
        Action act = () => new ConvexPolygon(vertices);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    public static IEnumerable<TheoryDataRow<Vector2[], string>> CcwTriangleCases()
    {
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(1f, 2f)
            ],
            "Right triangle at origin"
        );

        yield return new(
            [
                new Vector2(-3f, -1f),
                new Vector2(0f, -1f),
                new Vector2(-1.5f, 2f)
            ],
            "Translated isosceles triangle"
        );

        yield return new(
            [
                new Vector2(1f, 1f),
                new Vector2(4f, 2f),
                new Vector2(2f, 5f)
            ],
            "Scalene triangle"
        );
    }

    [Theory(DisplayName = "CCW triangles are accepted and strictly convex")]
    [MemberData(nameof(CcwTriangleCases))]
    public void Constructor_CcwTriangles_AreAccepted(Vector2[] vertices, string scenario)
    {
        var polygon = new ConvexPolygon(vertices);

        polygon.Count.Should().Be(3, because: scenario);
    }

    public static IEnumerable<TheoryDataRow<Vector2[], string>> NonCcwTriangleCases()
    {
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(1f, 2f),
                new Vector2(2f, 0f)
            ],
            "Clockwise triangle at origin"
        );

        yield return new(
            [
                new Vector2(-3f, -1f),
                new Vector2(-1.5f, 2f),
                new Vector2(0f, -1f)
            ],
            "Clockwise translated isosceles triangle"
        );

        yield return new(
            [
                new Vector2(1f, 1f),
                new Vector2(2f, 5f),
                new Vector2(4f, 2f)
            ],
            "Clockwise scalene triangle"
        );
    }

    [Theory(DisplayName = "Clockwise triangles throw")]
    [MemberData(nameof(NonCcwTriangleCases))]
    public void Constructor_NonCcwTriangles_Throw(Vector2[] vertices, string scenario)
    {
        Action act = () => new ConvexPolygon(vertices);

        act.Should().Throw<ArgumentException>(because: scenario);
    }


    public static IEnumerable<TheoryDataRow<Vector2[], AlignedRectangle, string>> ValidConvexPolygonCases()
    {
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            ],
            new AlignedRectangle(new Vector2(0f, 0f), 1f, 1f),
            "Unit square with bottom-left at origin"
        );

        yield return new(
            [
                new Vector2(-0.5f, -0.5f),
                new Vector2(0.5f, -0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-0.5f, 0.5f)
            ],
            new AlignedRectangle(new Vector2(-0.5f, -0.5f), 1f, 1f),
            "Unit square centered on origin"
        );

        yield return new(
            [
                new Vector2(2f, 1f),
                new Vector2(8f, 1f),
                new Vector2(8f, 4f),
                new Vector2(2f, 4f)
            ],
            new AlignedRectangle(new Vector2(2f, 1f), 6f, 3f),
            "Rectangle"
        );

        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(3f, 0f),
                new Vector2(4f, 2f),
                new Vector2(2f, 4f),
                new Vector2(-1f, 2f)
            ],
            new AlignedRectangle(new Vector2(-1f, 0f), 5f, 4f),
            "Convex pentagon"
        );
    }

    [Theory(DisplayName = "Unit squares, rectangle, and convex pentagon are accepted")]
    [MemberData(nameof(ValidConvexPolygonCases))]
    public void Constructor_ValidConvexPolygons_AreAccepted(Vector2[] vertices,
                                                            AlignedRectangle expectedBounds,
                                                            string scenario)
    {
        var polygon = new ConvexPolygon(vertices);

        polygon.Count.Should().Be(vertices.Length, because: scenario);
        polygon.Bounds.Should().Be(expectedBounds, because: scenario);
    }

    public static IEnumerable<TheoryDataRow<Vector2[], Vector2[], string>> ValidConvexPolygonNormalsCases()
    {
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            ],
            [
                -Vector2.UnitY,
                Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitX
            ],
            "Unit square"
        );

        yield return new(
            [
                new Vector2(2f, 1f),
                new Vector2(8f, 1f),
                new Vector2(8f, 4f),
                new Vector2(2f, 4f)
            ],
            [
                -Vector2.UnitY,
                Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitX
            ],
            "Rectangle"
        );

        var height = MathF.Sqrt(3f);
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(1f, height)
            ],
            [
                -Vector2.UnitY,
                Vector2.Normalize(new Vector2(height, 1f)),
                Vector2.Normalize(new Vector2(-height, 1f))
            ],
            "Equilateral triangle"
        );
    }

    [Theory(DisplayName = "Simple convex polygons expose expected outward edge normals")]
    [MemberData(nameof(ValidConvexPolygonNormalsCases))]
    public void EdgeNormals(Vector2[] vertices,
                            Vector2[] expectedNormals,
                            string scenario)
    {
        var polygon = new ConvexPolygon(vertices);

        polygon.EdgeNormals.Length.Should().Be(expectedNormals.Length, because: scenario);

        for (var i = 0; i < expectedNormals.Length; i++)
        {
            polygon.EdgeNormals[i].Should().Be(expectedNormals[i], because: $"{scenario} edge {i}");
        }
    }

    [Theory(DisplayName = "Reversing a valid convex polygon winding throws")]
    [MemberData(nameof(ValidConvexPolygonCases))]
    public void Constructor_ReversedValidConvexPolygons_Throw(Vector2[] vertices,
                                                              AlignedRectangle _,
                                                              string scenario)
    {
        var reversed = vertices.Reverse().ToArray();
        Action act = () => new ConvexPolygon(reversed);

        act.Should().Throw<ArgumentException>(because: $"reversing winding should invalidate {scenario}");
    }


    public static IEnumerable<TheoryDataRow<Vector2[], string>> InvalidPolygonCases()
    {
        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(4f, 0f),
                new Vector2(2f, 1f),
                new Vector2(4f, 4f),
                new Vector2(0f, 4f)
            ],
            "Concave pentagon"
        );

        yield return new(
            [
                new Vector2(0f, 0f),
                new Vector2(-1f, 2f),
                new Vector2(2f, 4f),
                new Vector2(4f, 2f),
                new Vector2(3f, 0f)
            ],
            "Convex pentagon with clockwise winding"
        );
    }

    [Theory(DisplayName = "Concave and clockwise convex pentagons throw")]
    [MemberData(nameof(InvalidPolygonCases))]
    public void Constructor_InvalidPentagons_Throw(Vector2[] vertices, string scenario)
    {
        Action act = () => new ConvexPolygon(vertices);

        act.Should().Throw<ArgumentException>(because: scenario);
    }
}
