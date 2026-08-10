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
    private const float PolygonVertexRadius = 4;

    private static readonly Vector2 PlayerStart = new(WindowWidth / 2f, WindowHeight / 2f);

    private readonly Sim _sim;
    private readonly CircleSystem _circleSystem = new();
    private readonly ConvexPolygonSystem _polygonSystem = new();
    private readonly PlayerAgent _player;
    private readonly List<IEntity> _polygons = [];
    private readonly ulong _frequency;
    private readonly double _tickInterval;

    private List<Vector2>? _currentPolygonVertices;
    private ulong _lastTickCounter;

    public PolygonsGame()
    {
        _sim = new Sim();
        _sim.AddSystems(new Spatial2DSystem(),
                        _circleSystem,
                        _polygonSystem,
                        new CircleConvexPolygonUpdateResolver(),
                        new ScreenBoundsConstraint(WindowWidth, WindowHeight));

        _player = new PlayerAgent(PlayerSpeed, PlayerSprintMultiplier);
        _sim.InitEntities((_player, [
                                        new Spatial2DSnapshot(PlayerStart),
                                        new CircleSnapshot(new Circle(PlayerStart, PlayerRadius))
                                    ]));

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

        if (type == SDL.EventType.MouseButtonDown &&
            e.Button.Button == SDL.ButtonLeft)
        {
            _currentPolygonVertices?.Add(new(e.Button.X, e.Button.Y));
            return false;
        }

        if (type != SDL.EventType.KeyDown)
        {
            return false;
        }

        switch (e.Key.Key)
        {
            case SDL.Keycode.Escape:
            case SDL.Keycode.Q:
                return true;
            case SDL.Keycode.P:
                EndCurrentPolygon();
                _currentPolygonVertices = [];
                break;
            case SDL.Keycode.Space:
                EndCurrentPolygon();
                _currentPolygonVertices = null;
                break;
        }

        return false;
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

        foreach (var polygon in _polygons)
        {
            renderer.DrawPolygon(_polygonSystem.GetTypedState(polygon),
                                 GpuColor.FromBytes(128, 255, 128));
        }

        if (_currentPolygonVertices is not null)
        {
            foreach (var vertex in _currentPolygonVertices)
            {
                renderer.FillCircle(vertex,
                                    PolygonVertexRadius,
                                    GpuColor.FromBytes(128, 255, 128));
            }
        }

        renderer.EndFrame();
    }

    private void EndCurrentPolygon()
    {
        if (_currentPolygonVertices is not { Count: >= 3 } vertices)
        {
            return;
        }

        ConvexPolygon polygon;
        try
        {
            polygon = new ConvexPolygon(vertices);
        }
        catch (ArgumentException)
        {
            try
            {
                polygon = new ConvexPolygon(Enumerable.Reverse(vertices));
            }
            catch (ArgumentException exception)
            {
                SDL.LogWarn(SDL.LogCategory.Application,
                            $"Could not add polygon: {exception.Message}");
                return;
            }
        }

        var polygonEntity = new Entity($"Polygon {_polygons.Count + 1}");
        _sim.AddEntity(polygonEntity, new ConvexPolygonSnapshot(polygon));
        _polygons.Add(polygonEntity);
    }
}
