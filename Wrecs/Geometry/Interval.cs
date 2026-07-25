namespace Wrecs.Geometry;

/// <summary>
/// A closed one-dimensional interval.
/// </summary>
public readonly record struct Interval
{
    public Interval(float min, float max)
    {
        if (max < min)
            throw new ArgumentException("Max must be greater than or equal to Min.", nameof(max));

        Min = min;
        Max = max;
    }

    public float Min { get; }
    public float Max { get; }
    public float Length => Max - Min;

    public bool Contains(float value) =>
        value >= Min && value <= Max;
}
