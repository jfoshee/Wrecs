using System.Numerics;
using SDL3;
using Wrecs.Core;
using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Game.Polygons;

class PolygonsGame
{
    public const int WindowWidth = 800;
    public const int WindowHeight = 800;

    private const int TickRate = 60;
    private const float PlayerRadius = 12;
    private const float PlayerSpeed = 4;
    private const float PlayerSprintMultiplier = 4;

    private static readonly Vector2 PlayerStart = new(WindowWidth / 2f, WindowHeight / 2f);

    private readonly Sim _sim;
    private readonly CircleSystem _circleSystem = new();
    private readonly ConvexPolygonSystem _polygonSystem = new();
    private readonly PlayerAgent _player;
    private readonly IEntity _polygon;
    private readonly ulong _frequency;
    private readonly double _tickInterval;

    private ulong _lastTickCounter;

    public PolygonsGame()
    {
        _sim = new Sim();
        _sim.AddSystems(new Spatial2DSystem(),
                        _circleSystem,
                        _polygonSystem,
                        new ScreenBoundsConstraint(WindowWidth, WindowHeight));

        _player = new PlayerAgent(PlayerSpeed, PlayerSprintMultiplier);
        _polygon = new Entity("Polygon");
        _sim.InitEntities((_player, [
                                        new Spatial2DSnapshot(PlayerStart),
                                        new CircleSnapshot(new Circle(PlayerStart, PlayerRadius))
                                    ]),
                           (_polygon, [new ConvexPolygonSnapshot(new ConvexPolygon(new Vector2[]
                           {
                               new(100, 100),
                               new(200, 100),
                               new(200, 200),
                               new(100, 200)
                           }))])
                           );

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

        return type == SDL.EventType.KeyDown &&
               (e.Key.Key == SDL.Keycode.Escape || e.Key.Key == SDL.Keycode.Q);
    }

    public void UpdateAndRender(PolygonsGpuRenderer renderer)
    {
        _player.HandleKeyboard();

        var currentCounter = SDL.GetPerformanceCounter();
        if (currentCounter - _lastTickCounter >= _tickInterval)
        {
            _sim.Tick();
            _lastTickCounter = currentCounter;
        }

        renderer.BeginFrame(GpuColor.FromBytes(8, 13, 28));

        var playerCircle = _circleSystem.GetTypedState(_player).Circle;
        renderer.FillCircle(playerCircle.Center,
                            playerCircle.Radius,
                            GpuColor.FromBytes(255, 128, 128));
        renderer.DrawPolygon(_polygonSystem.GetTypedState(_polygon),
                             GpuColor.FromBytes(128, 255, 128));

        renderer.EndFrame();
    }
}
