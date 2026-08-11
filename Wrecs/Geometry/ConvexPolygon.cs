namespace Wrecs.Geometry;

public sealed class ConvexPolygon
{
    private readonly Vector2[] _vertices;
    private readonly Vector2[] _edgeNormals;

    public ReadOnlySpan<Vector2> Vertices => _vertices;
    public ReadOnlySpan<Vector2> EdgeNormals => _edgeNormals;

    public AlignedRectangle Bounds { get; }

    public int Count => _vertices.Length;

    public ConvexPolygon(IEnumerable<Vector2> vertices)
    {
        _vertices = [.. vertices];
        ArgumentOutOfRangeException.ThrowIfLessThan(_vertices.Length, 3);

        ValidateStrictlyConvex(_vertices);

        _edgeNormals = new Vector2[_vertices.Length];

        var min = _vertices[0];
        var max = _vertices[0];

        for (var i = 0; i < _vertices.Length; i++)
        {
            var current = _vertices[i];
            var next = _vertices[(i + 1) % _vertices.Length];
            var edge = next - current;

            // For a CCW polygon, the right-hand normal points outward.
            _edgeNormals[i] = Vector2.Normalize(new(edge.Y, -edge.X));

            min = Vector2.Min(min, current);
            max = Vector2.Max(max, current);
        }

        Bounds = AlignedRectangle.FromMinMax(min, max);
    }

    /// <summary>
    /// Creates a convex polygon from the rectangle's four corners.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The rectangle does not have positive width and height.
    /// </exception>
    public static ConvexPolygon FromRectangle(AlignedRectangle rectangle)
    {
        if (rectangle.Width <= 0f || rectangle.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(rectangle),
                                                  "Rectangle width and height must be positive.");
        }

        return new ConvexPolygon(rectangle.Corners);
    }

    public Vector2 GetVertex(int index) => _vertices[index];

    public LineSegment GetEdge(int index) =>
        new(_vertices[index], _vertices[(index + 1) % _vertices.Length]);

    private static void ValidateStrictlyConvex(ReadOnlySpan<Vector2> vertices)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Length];
            var c = vertices[(i + 2) % vertices.Length];

            var cross = Vector2.Cross(b - a, c - b);

            if (cross <= 0f)
            {
                throw new ArgumentException(
                    "Vertices must form a strictly convex counterclockwise polygon.");
            }
        }
    }
}
