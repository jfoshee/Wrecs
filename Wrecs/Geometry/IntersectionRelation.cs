namespace Wrecs.Geometry;

/// <summary>
/// Describes the spatial relationship between two geometric objects.
/// </summary>
public enum IntersectionRelation
{
    /// <summary>
    /// The objects share no points.
    /// </summary>
    Disjoint,

    /// <summary>
    /// The objects share boundary points, but do not overlap in their interiors.
    /// </summary>
    Touching,

    /// <summary>
    /// The objects overlap in their interiors.
    /// </summary>
    Overlapping
}
