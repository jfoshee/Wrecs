using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;
using Wrecs.Geometry;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class MazeLevel
{
    private const float PlayerSize = 20;
    private const float PlayerSpeed = 8;
    private const float PlayerSprintMultiplier = 8;
    private static readonly Vector2 PlayerStart = new(1 + PlayerSize / 2, 1 + PlayerSize / 2);
    private const float MazeScale = 40;
    private const float GoalSize = 30f;

    // private const float MazeSize = MazeCells * MazeScale;
    // public const int WindowPixels = (int)MazeSize + 1;
    public const int WindowPixels = 800;

    private readonly Sim _sim;
    private readonly ScaledMaze _maze;
    private readonly StrongBox<bool> _isGameEnded = new(false);
    private readonly PlayerAgent _player;
    private readonly GoalEntity _goal;
    private readonly ulong _frequency;
    private readonly double _tickInterval;

    private ulong _lastTickCounter;

    public bool IsGameEnded => _isGameEnded.Value;

    public MazeLevel(int MazeCells)
    {
        var bounds = (MazeCells * MazeScale) + 1;
        var baseMaze = MazeGenerator.Generate(MazeCells, MazeCells);
        _maze = new ScaledMaze(baseMaze, MazeScale);

        _sim = new Sim();
        _sim.AddSystems(new Spatial2DSystem(),
                        new GameBoundsConstraint(bounds, bounds, PlayerSize / 2),
                        new CircleSystem(),
                        new CircleMazeWallsUpdateResolver(_maze.GetWalls()),
                        new AlignedRectangleSystem(),
                        new AlignedRectangleCollisionEventSystem(),
                        new PlayerGoalCollisionHandler(),
                        new EndGameLatchSystem(_isGameEnded));

        _player = new PlayerAgent(PlayerSpeed, PlayerSprintMultiplier);
        _goal = new GoalEntity();
        var goalPosition = _maze.GoalPosition;
        _sim.InitEntities((_player, [
                                        new Spatial2DSnapshot(PlayerStart),
                                        new AlignedRectangleSnapshot(AlignedRectangle.Centered(PlayerStart, PlayerSize, PlayerSize)),
                                        new CircleSnapshot(new(PlayerStart, PlayerSize / 2))
                                    ]),
                          (_goal, [new Spatial2DSnapshot(goalPosition), new AlignedRectangleSnapshot(new(goalPosition, GoalSize, GoalSize))]));
        // Link rectangle and player position to circle position
        _sim.AddLinkage(new(SourceEntity: _player,
                            SourceSystem: _sim.GetSystem<CircleSystem>(),
                            TargetEntity: _player,
                            TargetSystem: _sim.GetSystem<Spatial2DSystem>()));
        _sim.AddLinkage(new(SourceEntity: _player,
                            SourceSystem: _sim.GetSystem<CircleSystem>(),
                            TargetEntity: _player,
                            TargetSystem: _sim.GetSystem<AlignedRectangleSystem>()));

        _lastTickCounter = SDL.GetPerformanceCounter();
        _frequency = SDL.GetPerformanceFrequency();
        _tickInterval = _frequency / 30.0;
    }

    public bool HandleEvent(SDL.Event e)
    {
        var type = (SDL.EventType)e.Type;
        if (type == SDL.EventType.Quit)
        {
            return true;
        }

        if (type != SDL.EventType.KeyDown && type != SDL.EventType.KeyUp)
        {
            return false;
        }

        switch (e.Key.Key)
        {
            case SDL.Keycode.Q:
            case SDL.Keycode.Escape:
                return true;
            case SDL.Keycode.C:
                _sim.DisableSystem<CircleMazeWallsUpdateResolver>();
                break;
            case SDL.Keycode.K:
                _sim.EnableSystem<CircleMazeWallsUpdateResolver>();
                break;
        }

        return false;
    }

    public void UpdateAndRender(MazeGpuRenderer renderer)
    {
        _player.HandleKeyboard();

        var currentCounter = SDL.GetPerformanceCounter();
        renderer.BeginFrame(GpuColor.FromBytes(100, 149, 237));

        var goalRect = _sim.GetSystem<AlignedRectangleSystem>().GetTypedState(_goal).Rectangle;
        renderer.FillRectangle(goalRect.BottomLeft.X,
                               goalRect.BottomLeft.Y,
                               goalRect.Width,
                               goalRect.Height,
                               GpuColor.FromBytes(255, 215, 0));

        var playerRect = _sim.GetSystem<AlignedRectangleSystem>().GetTypedState(_player).Rectangle;
        renderer.FillRectangle(playerRect.BottomLeft.X,
                               playerRect.BottomLeft.Y,
                               playerRect.Width,
                               playerRect.Height,
                               GpuColor.FromBytes(255, 0, 0));

        var playerCircle = _sim.GetSystem<CircleSystem>().GetTypedState(_player).Circle;
        renderer.FillCircle(playerCircle.Center,
                            playerCircle.Radius,
                            GpuColor.FromBytes(255, 128, 128));

        var playerPosition = _sim.GetSystem<Spatial2DSystem>().GetTypedState(_player).Position;
        renderer.FillRectangle(playerPosition.X - 2,
                               playerPosition.Y - 2,
                               4,
                               4,
                               GpuColor.FromBytes(127, 255, 127));
        foreach (var wall in _maze.GetWalls())
        {
            renderer.DrawLine(wall.Start, wall.End, GpuColor.FromBytes(255, 255, 255));
        }

        renderer.EndFrame();

        if (currentCounter - _lastTickCounter >= _tickInterval)
        {
            _sim.Tick();
            _lastTickCounter = currentCounter;
        }
    }
}
