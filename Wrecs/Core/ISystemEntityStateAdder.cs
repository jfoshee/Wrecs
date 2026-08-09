namespace Wrecs.Core;

/// <summary>
/// A system that supports adding an entity after its initial entity set has been initialized.
/// </summary>
public interface ISystemEntityStateAdder : ISystem
{
    void AddEntity(IEntity entity, IStateSnapshot[] initialStates);
}

/// <summary>
/// Adapts untyped initial state supplied by <see cref="Sim.AddEntity"/> to a system's
/// typed initial state.
/// </summary>
public interface ISystemEntityStateAdder<TStateSnapshot> : ISystemEntityStateAdder
    where TStateSnapshot : struct, IStateSnapshot
{
    void AddEntity(IEntity entity, TStateSnapshot? initialState);

    void ISystemEntityStateAdder.AddEntity(IEntity entity, IStateSnapshot[] initialStates)
    {
        var initialState = initialStates
            .OfType<TStateSnapshot>()
            .Select(state => (TStateSnapshot?)state)
            .FirstOrDefault();

        AddEntity(entity, initialState);
    }
}
