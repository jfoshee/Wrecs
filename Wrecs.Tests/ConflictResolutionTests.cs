namespace Wrecs.Tests;

public class ConflictResolutionTests
{
    [Fact(DisplayName = "Conflict resolution keeps original update when no conflict is resolved")]
    public void NoConflictResolved_AppliesOriginalUpdate()
    {
        var (sim, system, entities) = CreateSimWithUpdates(5);
        sim.AddSystem(new ReplaceValueResolver(from: 999, to: 0));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(5);
    }

    [Fact(DisplayName = "Conflict resolution replaces one update when conflict is resolved")]
    public void OneUpdateResolved_AppliesResolvedUpdate()
    {
        var (sim, system, entities) = CreateSimWithUpdates(1);
        sim.AddSystem(new ReplaceValueResolver(from: 1, to: 10));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(10);
    }

    [Fact(DisplayName = "Conflict resolution can resolve the first update in a list")]
    public void FirstUpdateResolved_AppliesResolvedAndUnchangedUpdates()
    {
        var (sim, system, entities) = CreateSimWithUpdates(1, 2, 3);
        sim.AddSystem(new ReplaceValueResolver(from: 1, to: 10));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(10);
        system.GetValue(entities[1]).Should().Be(2);
        system.GetValue(entities[2]).Should().Be(3);
    }

    [Fact(DisplayName = "Conflict resolution can resolve the last update in a list")]
    public void LastUpdateResolved_AppliesResolvedAndUnchangedUpdates()
    {
        var (sim, system, entities) = CreateSimWithUpdates(1, 2, 3);
        sim.AddSystem(new ReplaceValueResolver(from: 3, to: 30));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(1);
        system.GetValue(entities[1]).Should().Be(2);
        system.GetValue(entities[2]).Should().Be(30);
    }

    [Fact(DisplayName = "Conflict resolution can resolve every update in a 3+ update list")]
    public void EveryUpdateResolved_ForThreeOrMoreUpdates()
    {
        var (sim, system, entities) = CreateSimWithUpdates(1, 2, 3, 4);
        sim.AddSystem(new AddOffsetResolver(offset: 100));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(101);
        system.GetValue(entities[1]).Should().Be(102);
        system.GetValue(entities[2]).Should().Be(103);
        system.GetValue(entities[3]).Should().Be(104);
    }

    [Fact(DisplayName = "Multiple resolvers can resolve the same update in sequence")]
    public void MultipleResolvers_CanResolveSameUpdate()
    {
        var (sim, system, entities) = CreateSimWithUpdates(1);
        sim.AddSystems(
            new ReplaceValueResolver(from: 1, to: 10),
            new ReplaceValueResolver(from: 10, to: 99));

        sim.Tick();

        system.GetValue(entities[0]).Should().Be(99);
    }

    private static (Sim sim, ConflictStateSystem system, ConflictEntity[] entities) CreateSimWithUpdates(params int[] values)
    {
        var sim = new Sim();
        var system = new ConflictStateSystem();
        var entities = values
            .Select((_, i) => new ConflictEntity(EntityId.Next(), $"Entity{i + 1}"))
            .ToArray();

        var updateSets = values
            .Select((value, i) =>
            {
                var update = new EntityUpdate<ConflictSnapshot>(entities[i], new ConflictSnapshot(value));
                return new UpdateSet([update]);
            })
            .ToArray();

        sim.AddSystems(system, new FixedConflictUpdateSource(updateSets));

        var initialEntities = entities
            .Select(entity => (entity: (IEntity)entity, initialStates: Array.Empty<IStateSnapshot>()))
            .ToArray();
        sim.InitEntities(initialEntities);

        return (sim, system, entities);
    }

    private interface IConflictEntity : IEntity;

    private sealed record ConflictEntity(int Id, string Name = "ConflictEntity") : IConflictEntity;

    private readonly record struct ConflictSnapshot(int Value) : IStateSnapshot<ConflictStateSystem>;

    private sealed class ConflictStateSystem :
        ISystemWithEntities<IConflictEntity, ConflictSnapshot>,
        ISystemUpdateAcceptor<ConflictSnapshot>
    {
        private readonly Dictionary<IEntity, int> _states = [];

        public void InitEntities(params (IEntity entity, ConflictSnapshot? initialState)[] initialEntities)
        {
            _states.Clear();
            foreach (var (entity, initialState) in initialEntities)
            {
                _states[entity] = initialState?.Value ?? 0;
            }
        }

        public IReadOnlyList<IEntity> GetEntities() => [.. _states.Keys];

        public ConflictSnapshot GetTypedState(IEntity entity) =>
            _states.TryGetValue(entity, out var value) ? new ConflictSnapshot(value) : default;

        public void ApplyUpdates(IEnumerable<EntityUpdate<ConflictSnapshot>> updates)
        {
            foreach (var update in updates)
                _states[update.Entity] = update.State.Value;
        }

        public int GetValue(IEntity entity) => _states[entity];
    }

    private sealed class FixedConflictUpdateSource(params UpdateSet[] updateSets) : ISystemSharedUpdates
    {
        public IEnumerable<UpdateSet> PrepareSharedUpdates() => updateSets;
    }

    private sealed class ReplaceValueResolver(int from, int to) : ISystemUpdateResolver
    {
        public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
        {
            var update = proposedUpdateSet.Updates.OfType<EntityUpdate<ConflictSnapshot>>().Single();
            if (update.State.Value != from)
                return new ResolutionResult(false, proposedUpdateSet);

            var resolvedUpdate = new EntityUpdate<ConflictSnapshot>(
                update.Entity,
                new ConflictSnapshot(to));

            return new ResolutionResult(true, new UpdateSet([resolvedUpdate]));
        }
    }

    private sealed class AddOffsetResolver(int offset) : ISystemUpdateResolver
    {
        public ResolutionResult ResolveUpdates(UpdateSet proposedUpdateSet)
        {
            var update = proposedUpdateSet.Updates.OfType<EntityUpdate<ConflictSnapshot>>().Single();
            var resolvedUpdate = new EntityUpdate<ConflictSnapshot>(
                update.Entity,
                new ConflictSnapshot(update.State.Value + offset));

            return new ResolutionResult(true, new UpdateSet([resolvedUpdate]));
        }
    }
}
