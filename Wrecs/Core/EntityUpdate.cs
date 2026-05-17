namespace Wrecs.Core;

public interface IEntityUpdate
{
    IEntity Entity { get; }
    // IStateSnapshot State { get; }
}

// public record EntityUpdate<TSystem, TStateSnapshot>(IEntity Entity, TStateSnapshot State) : IEntityUpdate
//     where TSystem : ISystem
//     where TStateSnapshot : IStateSnapshot<TSystem>;

public record EntityUpdate<TStateSnapshot>(IEntity Entity, TStateSnapshot State) : IEntityUpdate
    where TStateSnapshot : IStateSnapshot;
