namespace Wrecs.Geometry;

public static class BoundingRectangle
{
    public static AlignedRectangle From(AlignedRectangle alignedRectangle, float? rotationRadians)
    {
        if (rotationRadians is null || rotationRadians == 0)
        {
            return alignedRectangle;
        }
        return new RotatedRectangle(alignedRectangle, rotationRadians.Value).BoundingRectangle;
    }
}