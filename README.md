# Wrecs

An Event-Driven, Turn-Based Entity Component System (ECS) designed for determinism, agent reasoning, and complex state coordination.

## Overview

Unlike traditional archetype or sparse set ECS frameworks (which prioritize contiguous memory layouts and purely data-driven iterations), **Wrecs** is optimized for rich logic, transaction-like interactions, and strict tick phasing. It cleanly separates _state_, _computation_, and _mutation_ using a deferred update model.

This makes Wrecs highly suited for:

- Turn-based games and simulations.
- Commercial or economic simulations requiring transactional atomicity (e.g., transfers of currency/resources).
- Autonomous agent environments where entities require a static, read-only snapshot of the world while "thinking."

## High-Level Architecture

In Wrecs, data mutations are primarily deferred. A single simulation tick (`Sim.Tick()`) moves through a rigid set of phases:

1. **Wait and Inject (Initialization):** Before ticks begin, systems and requiring-entities are linked together using a built-in dependency injection interface.
2. **Preparation Phase:** Systems analyze their localized state and propose updates (both internal and shared multi-system transactions) safely, _before_ any actor logic acts upon the world.
3. **Agent Invocation Phase:** Agents (active entities) look at the world via read-only state snapshots, evaluate, and declare their **Intents**. Systems subsequently act as translators, converting these business logic intents into proposed state updates.
4. **Event Phase:** Systems can raise cross-cutting events. These are immediately flushed to and handled by any listening systems, allowing for side effects (like logging or visual updates).
5. **Update Phase:** All accumulated proposed updates (Internal updates, Agent intents, and Shared updates) are finally committed to the systems' underlying states, shifting the simulation into the next tick.

Because state isn't mutated in the middle of a tick, an agent evaluating the world will not read a partially updated state caused by another agent that happened to act earlier in the same phase.

## Data Structures and Interfaces

### Entities and State

- **`IEntity`**: The fundamental building block. Unlike Object-Oriented models, entities in Wrecs don't store their own data or components. An Entity is essentially just an `Id` and a human-readable `Name`.
- **`ISystem`**: The base interface for systems. In Wrecs, systems are the repositories for state. A generic `ISystem<TMarkerInterface, TStateSnapshot>` manages the mapping of entities to specific state structures.
- **`IStateSnapshot`**: An immutable snapshot of an entity's state for a given system. This ensures agents can read the world without risk of accidentally altering it outside of the rigid phase pipeline.

### Actors and Logic

- **`IAgent`**: A specialized `IEntity` that actively does things. Agents define logic that returns an `AgentIntent` when evaluated.
- **`IAgentContext`**: Passed to agents during their evaluation phase. Systems implementing **`ISystemAgentContextProvider`** enrich this context with whatever the agent needs to know (e.g., a spatial system provides surrounding positions; an inventory system provides current resources).
- **`AgentIntent` / `IAgentIntentAction`**: A declarative pattern describing _what_ an agent wants to do, decoupling the desire from the execution.
- **`ISystemAgentIntentTranslator`**: Systems intercept `IAgentIntentAction`s they know how to handle and translate them into a series of concrete state updates.

### Mutation and Transactions

State mutations happen via explicitly declared bundles instead of direct assignment operations.

- **`IEntityUpdate`**: A delayed application of a new state (`IStateSnapshot`) for a specific entity.
- **`UpdateSet`**: A grouping of `IEntityUpdate` records that conceptually represent an atomic transaction. If multiple systems coordinate on something (e.g., Deduct Money from System A, Give Item in System B), they are bundled up here.
- Application Lifecycle Interfaces for Systems:
  - **`ISystemInternalUpdatePreparer` / `IProposeUpdates`**: Hook for proposing updates during phase 1.
  - **`ISystemInternalUpdateApplier`**: Hook for committing strict internal state during phase 4.
  - **`ISystemUpdateAcceptor`**: Hook for receiving the big bucket of `IEntityUpdate` instances across the whole simulation and committing them to internal state.

### Communication and DI

- **`ISystemEventRaiser` / `ISystemEventHandler`**: A decoupled pub/sub pipeline ensuring systems can react to one-off scenarios globally without tightly coupling logic.
- **`IRequire<TSystem>`**: Indicates that a System or an Agent explicitly depends on another System being injected. The simulation layer automatically hooks these up. All dependency wiring is finalized on the initial un-paused tick via `Sim.EnsureDependenciesInjected()`.
