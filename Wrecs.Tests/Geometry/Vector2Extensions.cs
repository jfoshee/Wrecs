namespace Wrecs.Tests.Geometry;

public static class Vector2Extensions
{
    extension(Vector2 vector)
    {
        public Vector2 Round(int decimals)
        {
            var factor = MathF.Pow(10, decimals);
            return new Vector2(MathF.Round(vector.X * factor) / factor,
                               MathF.Round(vector.Y * factor) / factor);
        }
    }
}
