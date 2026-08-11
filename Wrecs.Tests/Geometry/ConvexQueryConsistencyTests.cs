using static System.MathF;
using Wrecs.Geometry;

namespace Wrecs.Tests.Geometry;

/// <summary>
/// Cross-checks convex collision queries using equivalent representations and
/// transformations instead of duplicating their geometry calculations in the
/// tests. Deterministic generated cases verify that specialized and general
/// query paths agree and that results preserve symmetry, rigid-transform, and
/// endpoint-order invariants. Explicitly constructed relationships keep the
/// scenarios understandable and avoid accidental near-tolerance ambiguity.
/// </summary>
public class ConvexQueryConsistencyTests
{
    [Fact(DisplayName = "Aligned and zero-rotation rectangle queries agree")]
    public void AlignedAgreement()
    {
        var random = new Random(731_943);

        for (var caseIndex = 0; caseIndex < 100; caseIndex++)
        {
            // Vary dimensions and positions while deliberately cycling through
            // five easy-to-picture relationships. This avoids a random suite
            // consisting almost entirely of separated rectangles.
            var first = new AlignedRectangle(new(1_000 + random.Next(100, 900),
                                                  10_000 + random.Next(1_000, 9_000)),
                                             random.Next(20, 100),
                                             random.Next(200, 1_000));
            var (second, scenario) = CreateAlignedCase(first,
                                                       caseIndex % 5,
                                                       random);
            var expected = first.GetIntersectionRelation(second);
            var rotatedFirst = new RotatedRectangle(first,
                                                    rotationRadians: 0f);
            var rotatedSecond = new RotatedRectangle(second,
                                                     rotationRadians: 0f);
            var details = $"{scenario}; case {caseIndex}; " +
                          $"first={Describe(first)}; second={Describe(second)}";

            rotatedFirst.GetIntersectionRelation(second)
                .Should().Be(expected, because: details);
            rotatedFirst.GetIntersectionRelation(rotatedSecond)
                .Should().Be(expected, because: details);
        }
    }

    [Fact(DisplayName = "Rotated rectangle relations are symmetric")]
    public void RelationSymmetry()
    {
        var random = new Random(492_817);

        for (var caseIndex = 0; caseIndex < 100; caseIndex++)
        {
            var center = new Vector2(1_000 + random.Next(100, 900),
                                     10_000 + random.Next(1_000, 9_000));
            var first = CreateRotatedRectangle(center,
                                               random,
                                               minimumWidth: 20,
                                               minimumHeight: 200);

            // Alternate between nearby shapes, which commonly overlap, and a
            // shape several heights away, which is visibly separated.
            var isNearby = caseIndex % 2 == 0;
            var offset = isNearby
                ? new Vector2(random.Next(-20, 21), random.Next(-100, 101))
                : new Vector2(random.Next(400, 700), random.Next(2_000, 3_000));
            var second = CreateRotatedRectangle(center + offset,
                                                random,
                                                minimumWidth: 30,
                                                minimumHeight: 300);
            var relation = first.GetIntersectionRelation(second);
            var scenario = isNearby
                ? "nearby rotated rectangles"
                : "widely separated rotated rectangles";

            second.GetIntersectionRelation(first)
                .Should().Be(relation,
                             because: $"{scenario}; case {caseIndex}; " +
                                      $"first center={first.Center}; " +
                                      $"second center={second.Center}");
        }
    }

    [Fact(DisplayName = "A shared rigid transform preserves rectangle relationships")]
    public void RigidTransform()
    {
        // These pairs depict a clear overlap, containment, and visible gap. None
        // sits near a classification boundary, so this tests transformation
        // invariance rather than tolerance policy.
        var cases = new[]
        {
            CreatePair(new(137, 1420),
                       34,
                       260,
                       17,
                       new(143, 1460),
                       51,
                       120,
                       -11,
                       IntersectionRelation.Overlapping,
                       "offset rectangles with crossing interiors"),
            CreatePair(new(237, 2420),
                       180,
                       700,
                       23,
                       new(241, 2440),
                       31,
                       140,
                       -7,
                       IntersectionRelation.Overlapping,
                       "small rectangle contained near the large rectangle's center"),
            CreatePair(new(337, 3420),
                       34,
                       260,
                       17,
                       new(817, 4960),
                       51,
                       120,
                       -11,
                       IntersectionRelation.Disjoint,
                       "rectangles separated by a visible diagonal gap")
        };
        var commonRotation = Angle.ToRadians(29);
        var commonTranslation = new Vector2(9_013, 70_140);

        foreach (var pair in cases)
        {
            pair.First.GetIntersectionRelation(pair.Second)
                .Should().Be(pair.Expected, because: pair.Scenario);
            var transformedFirst = ApplyRigidTransform(pair.First,
                                                       commonRotation,
                                                       commonTranslation);
            var transformedSecond = ApplyRigidTransform(pair.Second,
                                                        commonRotation,
                                                        commonTranslation);

            transformedFirst.GetIntersectionRelation(transformedSecond)
                .Should().Be(pair.Expected, because: pair.Scenario);
        }
    }

    [Fact(DisplayName = "Minimum translation moves overlaps to touching")]
    public void MinimumTranslation()
    {
        var random = new Random(284_963);

        for (var caseIndex = 0; caseIndex < 100; caseIndex++)
        {
            // Both rectangles are centered at nearly the same point. Their
            // interiors therefore overlap regardless of their random angles.
            var center = new Vector2(1_137 + random.Next(100, 900),
                                     20_140 + random.Next(1_000, 9_000));
            var first = CreateRotatedRectangle(center,
                                               random,
                                               minimumWidth: 40,
                                               minimumHeight: 400);
            var second = CreateRotatedRectangle(center +
                                                new Vector2(random.Next(-10, 11),
                                                            random.Next(-50, 51)),
                                                random,
                                                minimumWidth: 50,
                                                minimumHeight: 500);

            var overlaps = first.Overlaps(second,
                                          out var minimumTranslation);
            var movedBounds = first.OriginalAlignedRectangle with
            {
                BottomLeft = first.OriginalAlignedRectangle.BottomLeft +
                             minimumTranslation
            };
            var moved = new RotatedRectangle(movedBounds,
                                             first.RotationRadians);
            var details = $"case {caseIndex}; nearly coincident centers " +
                          $"{first.Center} and {second.Center}";

            overlaps.Should().BeTrue(because: details);
            minimumTranslation.Should().NotBe(Vector2.Zero, because: details);
            moved.GetIntersectionRelation(second)
                .Should().Be(IntersectionRelation.Touching, because: details);
        }
    }

    [Theory(DisplayName = "Reversing a segment preserves its rectangle relationship")]
    [InlineData(100, 1427, 180, 1427, "segment crosses both side edges")]
    [InlineData(154, 1327, 154, 1513, "segment lies on the right edge")]
    [InlineData(173, 1327, 173, 1513, "segment is visibly right of the rectangle")]
    public void SegmentDirection(float startX,
                                 float startY,
                                 float endX,
                                 float endY,
                                 string scenario)
    {
        // Define an easy-to-picture local setup, then rotate the entire setup so
        // the query cannot accidentally rely on horizontal or vertical edges.
        var aligned = AlignedRectangle.Centered(new(137, 1420),
                                                34,
                                                260);
        var angle = Angle.ToRadians(17);
        var rectangle = new RotatedRectangle(aligned, angle);
        var rotation = Matrix3x2.CreateRotation(angle, rectangle.Center);
        var start = Vector2.Transform(new(startX, startY), rotation);
        var end = Vector2.Transform(new(endX, endY), rotation);
        var forward = new LineSegment(start, end);
        var reverse = new LineSegment(end, start);

        rectangle.GetIntersectionRelation(reverse)
            .Should().Be(rectangle.GetIntersectionRelation(forward),
                         because: scenario);
    }

    [Theory(DisplayName = "Large-coordinate rotated rectangles distinguish shared edge, gap, and overlap")]
    [InlineData(0, IntersectionRelation.Touching, "right and left edges coincide")]
    [InlineData(8, IntersectionRelation.Disjoint, "an eight-unit gap separates the edges")]
    [InlineData(-8, IntersectionRelation.Overlapping, "the rectangles overlap by eight units")]
    public void LargeCoordinates(float edgeOffset,
                                 IntersectionRelation expected,
                                 string scenario)
    {
        // The second center lies along the first rectangle's rotated local X
        // axis. At offset zero, the first's right edge and second's left edge
        // occupy the same line segment.
        var center = new Vector2(1_000_137, 2_001_420);
        const float firstWidth = 340;
        const float secondWidth = 510;
        var angle = Angle.ToRadians(17);
        var localXAxis = new Vector2(Cos(angle), Sin(angle));
        var centerDistance = (firstWidth + secondWidth) / 2f + edgeOffset;
        var secondCenter = center + localXAxis * centerDistance;
        var first = new RotatedRectangle(AlignedRectangle.Centered(center,
                                                                  firstWidth,
                                                                  2_600),
                                         angle);
        var second = new RotatedRectangle(AlignedRectangle.Centered(secondCenter,
                                                                   secondWidth,
                                                                   1_200),
                                          angle);

        first.GetIntersectionRelation(second)
            .Should().Be(expected, because: scenario);
        second.GetIntersectionRelation(first)
            .Should().Be(expected, because: scenario);
    }

    private static (AlignedRectangle Rectangle, string Scenario) CreateAlignedCase(
        AlignedRectangle first,
        int relationship,
        Random random)
    {
        var secondWidth = random.Next(20, 100);
        var secondHeight = random.Next(200, 1_000);

        return relationship switch
        {
            0 => (new AlignedRectangle(new(first.Left + first.Width / 3f,
                                           first.Bottom + first.Height / 3f),
                                       secondWidth,
                                       secondHeight),
                  "interiors overlap near the first rectangle's lower-left quarter"),
            1 => (new AlignedRectangle(new(first.Left + first.Width / 4f,
                                           first.Bottom + first.Height / 4f),
                                       first.Width / 2f,
                                       first.Height / 2f),
                  "second rectangle is contained inside the first"),
            2 => (new AlignedRectangle(new(first.Right,
                                           first.Bottom + first.Height / 4f),
                                       secondWidth,
                                       first.Height / 2f),
                  "first right edge and second left edge are shared"),
            3 => (new AlignedRectangle(new(first.Right, first.Top),
                                       secondWidth,
                                       secondHeight),
                  "first top-right and second bottom-left corners coincide"),
            _ => (new AlignedRectangle(new(first.Right + 17,
                                           first.Bottom + first.Height / 4f),
                                       secondWidth,
                                       secondHeight),
                  "a visible horizontal gap separates the rectangles")
        };
    }

    private static RotatedRectangle CreateRotatedRectangle(Vector2 center,
                                                           Random random,
                                                           int minimumWidth,
                                                           int minimumHeight)
    {
        var aligned = AlignedRectangle.Centered(center,
                                                random.Next(minimumWidth,
                                                            minimumWidth + 100),
                                                random.Next(minimumHeight,
                                                            minimumHeight + 1_000));
        var rotation = Angle.ToRadians(random.Next(-70, 71));
        return new RotatedRectangle(aligned, rotation);
    }

    private static RectanglePair CreatePair(Vector2 firstCenter,
                                            float firstWidth,
                                            float firstHeight,
                                            float firstDegrees,
                                            Vector2 secondCenter,
                                            float secondWidth,
                                            float secondHeight,
                                            float secondDegrees,
                                            IntersectionRelation expected,
                                            string scenario) =>
        new(new RotatedRectangle(AlignedRectangle.Centered(firstCenter,
                                                          firstWidth,
                                                          firstHeight),
                                 Angle.ToRadians(firstDegrees)),
            new RotatedRectangle(AlignedRectangle.Centered(secondCenter,
                                                           secondWidth,
                                                           secondHeight),
                                 Angle.ToRadians(secondDegrees)),
            expected,
            scenario);

    private static RotatedRectangle ApplyRigidTransform(RotatedRectangle rectangle,
                                                        float rotationRadians,
                                                        Vector2 translation)
    {
        var rotation = Matrix3x2.CreateRotation(rotationRadians);
        var center = Vector2.Transform(rectangle.Center, rotation) + translation;
        var original = rectangle.OriginalAlignedRectangle;
        var transformedBounds = AlignedRectangle.Centered(center,
                                                          original.Width,
                                                          original.Height);
        return new RotatedRectangle(transformedBounds,
                                    rectangle.RotationRadians + rotationRadians);
    }

    private static string Describe(AlignedRectangle rectangle) =>
        $"({rectangle.Left}, {rectangle.Bottom}) {rectangle.Width}x{rectangle.Height}";

    private readonly record struct RectanglePair(RotatedRectangle First,
                                                 RotatedRectangle Second,
                                                 IntersectionRelation Expected,
                                                 string Scenario);
}
