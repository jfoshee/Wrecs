namespace Wrecs.Core;

/// <summary>
/// A set of updates that should be handled atomically.
/// Either the entire set of updates should be applied, or none of them.
/// </summary>
public record UpdateSet(IEnumerable<IEntityUpdate> Updates);
