namespace Wrecs.Geometry;

[Flags]
public enum RelativePosition
{
    None = 0,
    Inside = 1 << 0,
    Above = 1 << 1,
    Below = 1 << 2,
    Left = 1 << 3,
    Right = 1 << 4
}
