namespace Wrecs.Core;

/// <summary>
/// A System that has Entities. It should be initialized with Entity state and can
/// provide a snapshot of Entity state.
/// </summary>
public interface ISystemWithEntities : ISystemEntityStateInitializer, ISystemEntityStateProvider;
