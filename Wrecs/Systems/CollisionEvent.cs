using Wrecs.Core;

namespace Wrecs.Systems;

public record struct CollisionEvent(IEntity EntityA, IEntity EntityB) : IEvent;
