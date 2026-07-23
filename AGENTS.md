# AGENTS.md — Wrecs Codebase Guide

This file documents structural conventions and patterns for the Wrecs ECS. Read this before generating or modifying code.

---

## Project Layout

```
Wrecs/                         — core library
  Sim.cs                       — simulation root; owns the tick loop
  SimExtensions.cs             — extension methods on Sim
  Core/                        — interfaces and primitives only; no concrete logic
  Systems/                     — reusable concrete systems
    Commercial/                — domain-specific systems (commercial simulation)
Wrecs.Tests/                   — xUnit test project
  Monopoly/                    — Monopoly game implementation (integration example)
  Sandboxes/                   — scratch/exploratory tests
Wrecs.WebGL/                   — WebGL demo; separate concern, don't modify unless asked
```

New domain systems belong under `Wrecs/Systems/`. New core abstractions belong under `Wrecs/Core/`.

---

## Naming Conventions

| Thing                   | Convention                   | Example                               |
| ----------------------- | ---------------------------- | ------------------------------------- |
| System class            | `{Domain}System`             | `Spatial1DSystem`, `TurnSystem`       |
| System snapshot         | `{Domain}Snapshot`           | `Spatial1DSnapshot`, `TurnSnapshot`   |
| Entity marker interface | `I{Capability}` or `I{Role}` | `ISpatial1DEntity`, `ITakeTurns`      |
| Agent marker interface  | `I{Domain}Agent`             | `ISpatial1DAgent`, `ICommercialAgent` |
| Typed update record     | `{Domain}Update`             | `Spatial1DUpdate`                     |
| Intent action           | `{Verb}{Domain}Action`       | `Move1DAction`, `TakeOfferDecision`   |
| Event                   | `{Thing}{Event}Event`        | `WrapAround1DEvent`, `EndGameEvent`   |
| IRequire field          | `_{camelCaseSystemType}`     | `_spatial1dSystem`, `_turnSystem`     |

---

## Defining a Snapshot

Snapshots are **`record struct`** types (value types, immutable by convention). Associate them with their owning system via `IStateSnapshot<TSystem>`.

```csharp
public record struct TurnSnapshot(bool IsMyTurn, int Phase = 0)
    : IStateSnapshot<TurnSystem>;
```

- Always `record struct`, never `class` or plain `struct`.
- Declare them in the same file as the system that owns them.
- Implicit conversion operators are welcome when the snapshot wraps a primitive:

```csharp
public record struct Spatial1DSnapshot(Position Position) : IStateSnapshot<Spatial1DSystem>
{
    public static implicit operator int(Spatial1DSnapshot s) => s.Position;
    public static implicit operator Spatial1DSnapshot(int p) => new(p);
}
```

---

## Defining a System

Use `ISystemWithEntities<TMarkerInterface, TStateSnapshot>` as the base when the system tracks per-entity state. This composite interface provides a default `InitEntities` implementation that:

- Includes entities implementing `TMarkerInterface`, **or**
- Includes entities that carry an initial `TStateSnapshot` in their `IStateSnapshot[]` array.

Do not use `IEntity` as the marker interface; that defeats the purpose!

Minimal pattern:

```csharp
public interface IMyEntity : IEntity;
public record struct MySnapshot(MyState ...) : IStateSnapshot<MySystem>;
public class MySystem : ISystem<IMyEntity, MySnapshot>
{
    private readonly Dictionary<IEntity, MyState> _states = [];

    public void InitEntities(params (IEntity entity, MySnapshot? initialState)[] initialEntities)
    {
        _states.Clear();
        foreach (var (entity, initialState) in initialEntities)
            _states[entity] = initialState ?? default;
    }

    public IReadOnlyList<IEntity> GetEntities() => [..._states.Keys];

    public MySnapshot GetTypedState(IEntity entity) => new(_states[entity]);
}
```

### Adding Update Acceptance

To allow external sources (controllers, agents) to change state, also implement `ISystemUpdateAcceptor<TSnapshot>`:

```csharp
public void ApplyUpdates(IEnumerable<EntityUpdate<MySnapshot>> updates)
{
    foreach (var update in updates)
        _states[update.Entity] = update.State;
}
```

### Typed Update Record (optional but conventional)

When a system has a clear primary update type, define a named update record in the same file:

```csharp
public record MyUpdate : EntityUpdate<MySnapshot>
{
    public MyUpdate(IEntity entity, MyState state) : base(entity, new MySnapshot(state)) { }
}
```

---

## Defining a Controller

A controller is a system that emits cross-system updates. It implements `ISystemSharedUpdates` and typically depends on one or more systems via `IRequire<T>`.

```csharp
public class MyController : ISystemSharedUpdates, IRequire<MySystem>
{
    private MySystem? _mySystem;
    public void Inject(MySystem dep) => _mySystem = dep;

    public IEnumerable<UpdateSet> PrepareSharedUpdates()
    {
        var updates = _mySystem!.GetEntities()
            .Select(e => (IEntityUpdate)new MyUpdate(e, ComputeNewState(e)));
        yield return new UpdateSet(updates);
    }
}
```

A controller that affects multiple systems returns a single `UpdateSet` containing updates for all of them — this is what makes it an atomic transaction.

---

## Defining an Entity and Marker Interface

Entities implement `IEntity` plus any marker interfaces that grant them membership in systems:

```csharp
public interface IMyEntity : IEntity;  // marker — declare in the system's file

public class MyEntity(string name) : IEntity, IMyEntity
{
    public int Id { get; } = EntityId.Next();
    public string Name => name;
}
```

Always generate IDs with `EntityId.Next()` (thread-safe incrementing counter).

---

## Defining an Agent

An agent is an entity that implements `IAgent` and declares which snapshots it needs:

```csharp
public class MyAgent : IAgent, IMyEntity,
    IAgentRequireSnapshot<MySnapshot>,
    IAgentRequireSnapshot<TurnSnapshot>
{
    public int Id { get; } = EntityId.Next();
    public string Name => nameof(MyAgent);

    public AgentIntent GetIntent(IAgentContext context)
    {
        var turn = context.GetSnapshot<TurnSnapshot>();
        if (!turn.IsMyTurn) return AgentIntent.Empty;

        var state = context.GetSnapshot<MySnapshot>();
        // decide ...
        return new AgentIntent([new MyAction(...)]);
    }
}
```

- Return `AgentIntent.Empty` (not `null`) when the agent has nothing to do.
- Implement `IAgentRequireSnapshot<T>` for every snapshot type the agent reads from context.
- Agents live in test files or domain-specific files, not in `Core/`.

### Agent Marker Interface Convention

When a system should only serve agents of a specific domain, define a combined marker:

```csharp
public interface IMyAgent : IMyEntity, IAgent, IAgentRequireSnapshot<MySnapshot>;
```

---

## Defining an Intent Action

Actions are plain immutable data — no logic:

```csharp
public record struct Move1DAction(Vector Step) : IAgentIntentAction;
```

Use `record struct` for lightweight actions; `record class` is acceptable for actions with collections or complex state. Define actions in the same file as the system that translates them.

---

## Translating Intent (System Side)

A system that handles a specific action type implements `ISystemAgentIntentTranslator<TAction>`:

```csharp
public class MySystem :
    ISystem<IMyEntity, MySnapshot>,
    ISystemAgentIntentTranslator<MyAction>
{
    public UpdateSet TranslateIntent(IAgent agent, MyAction action)
    {
        var current = _states[agent];
        var next = ComputeNext(current, action);
        return new([new MyUpdate(agent, next)]);
    }
}
```

The `CanTranslate` and non-generic `Translate` methods are provided by the default interface implementation — do not override them.

---

## Events

Events are `record struct` types implementing `IEvent`:

```csharp
public record struct WrapAround1DEvent(IEntity Entity, int OldPosition, int NewPosition) : IEvent;
```

A system raises events by implementing `ISystemEventRaiser<T>`:

- Accumulate events in a private list during other phases.
- In `GetTypedEvents()`, copy the list, clear it, and return the copy.

```csharp
private readonly List<MyEvent> _events = [];

public IEnumerable<MyEvent> GetTypedEvents()
{
    var result = _events.ToList();
    _events.Clear();
    return result;
}
```

A system handles events by implementing `ISystemEventHandler<T>` and providing `HandleTyped(T e)`. The non-generic dispatch is provided by the default interface implementation.

---

## Dependency Injection (`IRequire<T>`)

When a system needs another system, declare the dependency via `IRequire<T>`:

```csharp
public class MySystem : ISystemSharedUpdates, IRequire<Spatial1DSystem>
{
    private Spatial1DSystem? _spatial1dSystem;
    public void Inject(Spatial1DSystem dep) => _spatial1dSystem = dep;
}
```

- Field naming convention: `_{camelCaseType}`.
- The injected field is nullable (`?`) until the first tick.
- Guard with `?? throw new InvalidOperationException(...)` if the dependency is required at runtime.
- Injection also works on entities (agents), not just systems — used when an agent needs direct system access.

---

## Initializing Entities

Pass all initial state for an entity as an `IStateSnapshot[]` array:

```csharp
sim.InitEntities(
    (player1, [new Spatial1DSnapshot(0), new TurnSnapshot(IsMyTurn: true)]),
    (player2, [new Spatial1DSnapshot(10)])
);
```

Each system's `InitEntities` filters this array for its own snapshot type. An entity with no relevant snapshot still gets registered if it implements the system's marker interface.

---

## Testing Conventions

- **Always** use `DisplayName` on `[Fact]`: `[Fact(DisplayName = "Human readable description")]`
- Use test harness classes (e.g., `CommercialSimHarness`) to set up common scenarios rather than repeating setup in each test.
- Use FluentAssertions: `value.Should().Be(expected)`.
- Test files mirror the domain: `TurnSystemTests.cs`, `InventorySystemTests.cs`, etc.
- Inline test-only types (agents, entities, systems) in the test file where they're used unless shared across multiple test files.

---

## What Goes Where

| Thing                                | Location                                        |
| ------------------------------------ | ----------------------------------------------- |
| Core interfaces and primitives       | `Wrecs/Core/`                                   |
| Reusable systems                     | `Wrecs/Systems/`                                |
| Domain-specific systems              | `Wrecs/Systems/{Domain}/` or `Wrecs/Systems/`   |
| Snapshot and action types            | Same file as the system that owns them          |
| Marker interfaces                    | Same file as the system that uses them          |
| Test harnesses and shared test types | `Wrecs.Tests/` root                             |
| Game-specific code (e.g. Monopoly)   | `Wrecs.Tests/Monopoly/` or similar subdirectory |
| Integration/demo code                | `Wrecs.WebGL/` (separate project)               |

---

## Common Pitfalls

- **Don't store mutable state in snapshots.** Snapshots are value types — copying is intentional.
- **Don't read updated state during `Prepare*` phases.** All reads should reflect the state at the start of the tick. Changes are not visible until phase 5.
- **Don't implement `CanTranslate` or the non-generic `Translate` manually** on `ISystemAgentIntentTranslator<T>` — the defaults handle dispatch correctly.
- **Don't implement `PopulateAgentContext` manually** on `ISystemAgentContextProvider<T>` — the default checks `IAgentRequireSnapshot<T>` automatically.
- **Don't implement the non-generic `ApplyUpdates` manually** on `ISystemUpdateAcceptor<T>` — the default filters by type.
