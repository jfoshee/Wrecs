using System.Numerics;
using SDL3;
using Wrecs.Core;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class ConfettiLevel
{
    public const int WindowPixels = 800;

    private const int TickRate = 60;
    private const int DefaultConfettiCount = 400;

    private readonly Sim _sim;
    private readonly Spatial2DSystem _spatial2dSystem;
    private readonly Velocity2DSystem _velocity2dSystem;
    private readonly List<ConfettiEntity> _confetti = [];
    private readonly Random _random = new();

    private readonly ulong _frequency;
    private readonly double _tickInterval;
    private ulong _lastTickCounter;

    public ConfettiLevel(int confettiCount = DefaultConfettiCount)
    {
        _sim = new Sim();
        _spatial2dSystem = new Spatial2DSystem();
        _velocity2dSystem = new Velocity2DSystem();

        _sim.AddSystems(_spatial2dSystem, _velocity2dSystem);

        var entities = new (IEntity entity, IStateSnapshot[] initialStates)[confettiCount];
        for (var i = 0; i < confettiCount; i++)
        {
            var entity = new ConfettiEntity(RandomColorChannel(), RandomColorChannel(), RandomColorChannel(), RandomSize());
            _confetti.Add(entity);

            var position = new Vector2(RandomX(), RandomTopY());
            var velocity = new Vector2(RandomXDrift(), RandomFallSpeed());
            entities[i] = (entity, [new Spatial2DSnapshot(position), new Velocity2DSnapshot(velocity)]);
        }

        _sim.InitEntities(entities);

        _frequency = SDL.GetPerformanceFrequency();
        _tickInterval = _frequency / (double)TickRate;
        _lastTickCounter = SDL.GetPerformanceCounter();
    }

    public bool HandleEvent(SDL.Event e)
    {
        var type = (SDL.EventType)e.Type;
        if (type == SDL.EventType.Quit)
        {
            return true;
        }

        if (type != SDL.EventType.KeyDown)
        {
            return false;
        }

        return e.Key.Key == SDL.Keycode.Escape || e.Key.Key == SDL.Keycode.Q;
    }

    public void UpdateAndRender(MazeGpuRenderer renderer)
    {
        renderer.BeginFrame(GpuColor.FromBytes(8, 13, 28));

        RecycleOffscreenConfetti();

        var now = SDL.GetPerformanceCounter();
        if (now - _lastTickCounter >= _tickInterval)
        {
            _sim.Tick();
            _lastTickCounter = now;
        }

        foreach (var piece in _confetti)
        {
            var pos = _spatial2dSystem.GetTypedState(piece).Position;
            renderer.FillRectangle(pos.X,
                                   pos.Y,
                                   piece.Size,
                                   piece.Size,
                                   GpuColor.FromBytes(piece.Red, piece.Green, piece.Blue));
        }

        renderer.EndFrame();
    }

    private void RecycleOffscreenConfetti()
    {
        List<Spatial2DUpdate>? spatialUpdates = null;
        List<Velocity2DUpdate>? velocityUpdates = null;

        foreach (var piece in _confetti)
        {
            var position = _spatial2dSystem.GetTypedState(piece).Position;
            if (position.Y <= WindowPixels + 2)
            {
                continue;
            }

            spatialUpdates ??= [];
            velocityUpdates ??= [];

            spatialUpdates.Add(new Spatial2DUpdate(piece, new Vector2(RandomX(), RandomTopY())));
            velocityUpdates.Add(new Velocity2DUpdate(piece, new Vector2(RandomXDrift(), RandomFallSpeed())));
        }

        if (spatialUpdates is not null)
        {
            _spatial2dSystem.ApplyUpdates(spatialUpdates);
        }

        if (velocityUpdates is not null)
        {
            _velocity2dSystem.ApplyUpdates(velocityUpdates);
        }
    }

    private byte RandomColorChannel() => (byte)_random.Next(30, 256);
    private float RandomSize() => 3f + (_random.NextSingle() * 6f);
    private float RandomX() => _random.NextSingle() * WindowPixels;
    private float RandomTopY() => -1f - (_random.NextSingle() * 100f);
    private float RandomXDrift() => (_random.NextSingle() * 0.4f) - 0.2f;
    private float RandomFallSpeed() => 0.25f + (_random.NextSingle() * 0.75f);
}
