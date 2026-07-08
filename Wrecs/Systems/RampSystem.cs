using Wrecs.Core;

namespace Wrecs.Systems;

public interface IRampEntity : IEntity;
public record struct RampSnapshot(float CurrentValue,
                                  float EndingValue,
                                  int TickCount) : IStateSnapshot<RampSystem>;
public record struct RampEvent(IEntity Entity, float Value) : IEvent;

public class RampSystem :
    ISystemWithInternalUpdates,
    ISystemWithEntities<IRampEntity, RampSnapshot>,
    ISystemEventRaiser<RampEvent>
{
    private record struct Ramp(float InitialValue, float EndingValue, int TickCount)
    {
        public float Step { get; } = (EndingValue - InitialValue) / TickCount;
    }

    private int _tick;
    private readonly Dictionary<IEntity, float> _values = [];
    private readonly Dictionary<IEntity, Ramp> _ramps = [];

    public void InitEntities(params (IEntity entity, RampSnapshot? initialState)[] initialEntities)
    {
        RampSnapshot defaultRamp = new(0, 1, 100);
        foreach (var (entity, initialState) in initialEntities)
        {
            var initialRamp = initialState ?? defaultRamp;
            _values[entity] = initialRamp.CurrentValue;
            _ramps[entity] = new(initialRamp.CurrentValue,
                                 initialRamp.EndingValue,
                                 initialRamp.TickCount);
        }
    }

    public IReadOnlyList<IEntity> GetEntities() => [.. _values.Keys];

    public RampSnapshot GetTypedState(IEntity entity) =>
        new(_values[entity], _ramps[entity].EndingValue, _ramps[entity].TickCount);

    public void PrepareInternalUpdates()
    {
        _tick++;
    }

    public void ApplyInternalUpdates()
    {
        foreach (var entity in GetEntities())
        {
            var ramp = _ramps[entity];
            _values[entity] = _tick >= ramp.TickCount
                ? ramp.EndingValue
                : (_values[entity] + ramp.Step);
        }
    }

    public IEnumerable<RampEvent> GetTypedEvents()
    {
        foreach (var entity in GetEntities())
        {
            yield return new(entity, _values[entity]);
        }
    }
}
