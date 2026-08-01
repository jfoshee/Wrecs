namespace Wrecs.Geometry;

public record struct Circle(Vector2 Center, float Radius)
{
    public readonly float Diameter => 2 * Radius;

    public readonly AlignedRectangle Bounds => AlignedRectangle.Centered(Center, Diameter);
}
