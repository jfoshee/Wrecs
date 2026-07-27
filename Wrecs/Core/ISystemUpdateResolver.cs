namespace Wrecs.Core;

public interface ISystemUpdateResolver : ISystem
{
    /// <summary>
    /// Resolves a set of proposed UpdateSet into a valid UpdateSet.
    /// </summary>
    ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet);
}

/// <summary>
/// Represents the result of resolving a proposed update set.
/// </summary>
/// <param name="ConflictResolved">Indicates that a conflict had to be resolved and the proposed update set has been modified.</param>
/// <param name="UpdateSet">The resolved update set. If there was no conflict it is the same as the proposed update set.</param>
public readonly record struct ResolutionResult(bool ConflictResolved, UpdateSet UpdateSet);
