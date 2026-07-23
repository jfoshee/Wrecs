using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wrecs.Geometry;

public static class Angle
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(float degrees) => degrees * (MathF.PI / 180f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull("degrees")]
    public static float? ToRadians(float? degrees) => degrees * (MathF.PI / 180f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(float radians) => radians * (180f / MathF.PI);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull("degrees")]
    public static float? ToDegrees(float? radians) => radians * (180f / MathF.PI);

    public static float Normalize(float radians)
    {
        // TODO: Handle angles outside -4PI to 4PI
        if (radians < 0)
        {
            return radians + 2 * MathF.PI;
        }
        else if (radians >= 2 * MathF.PI)
        {
            return radians - 2 * MathF.PI;
        }
        return radians;
    }
}
