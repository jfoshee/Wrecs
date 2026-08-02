using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;
using Wrecs.Systems;

namespace Wrecs.Game.Maze;

class MazeLevel
{
    private const float PlayerSize = 20;
    private const float PlayerSpeed = 8;
    private const float PlayerSprintMultiplier = 8;
    private static readonly Vector2 PlayerStart = new(1, 1);
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

    private readonly ulong _startCounter;
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
                                        new AlignedRectangleSnapshot(new(PlayerStart, PlayerSize, PlayerSize)),
                                        new CircleSnapshot(new(PlayerStart + new Vector2(PlayerSize / 2, PlayerSize / 2), PlayerSize / 2))
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

        _startCounter = SDL.GetPerformanceCounter();
        _lastTickCounter = _startCounter;
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
                _sim.DisableSystem<MazeWallsUpdateResolver>();
                break;
            case SDL.Keycode.K:
                _sim.EnableSystem<MazeWallsUpdateResolver>();
                break;
        }

        return false;
    }

    public void UpdateAndRender(nint renderer)
    {
        _player.HandleKeyboard();

        var currentCounter = SDL.GetPerformanceCounter();
        var elapsed = (currentCounter - _startCounter) / (double)_frequency;

        SDL.SetRenderDrawColor(renderer, 100, 149, 237, 255);
        SDL.RenderClear(renderer);

        // SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
        // foreach (var wall in _maze.GetWalls())
        // {
        //     SDL.RenderLine(renderer, wall.Start.X, wall.Start.Y, wall.End.X, wall.End.Y);
        // }

        var goalRect = _sim.GetSystem<AlignedRectangleSystem>().GetTypedState(_goal).Rectangle;
        var sdlGoalRect = new SDL.FRect
        {
            X = goalRect.BottomLeft.X,
            Y = goalRect.BottomLeft.Y,
            W = goalRect.Width,
            H = goalRect.Height,
        };
        SDL.SetRenderDrawColor(renderer, 255, 215, 0, 255);
        SDL.RenderFillRect(renderer, in sdlGoalRect);

        var playerRect = _sim.GetSystem<AlignedRectangleSystem>().GetTypedState(_player).Rectangle;
        var sdlPlayerRect = new SDL.FRect { X = playerRect.BottomLeft.X, Y = playerRect.BottomLeft.Y, W = playerRect.Width, H = playerRect.Height };
        SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
        SDL.RenderFillRect(renderer, in sdlPlayerRect);

        var playerCircle = _sim.GetSystem<CircleSystem>().GetTypedState(_player).Circle;
        var circleRows = (int)MathF.Ceiling(playerCircle.Radius * 2);
        Span<SDL.FRect> circleScanlines = stackalloc SDL.FRect[circleRows];
        for (var row = 0; row < circleRows; row++)
        {
            var y = row + 0.5f - playerCircle.Radius;
            var x = MathF.Sqrt((playerCircle.Radius * playerCircle.Radius) - (y * y));
            circleScanlines[row] = new SDL.FRect
            {
                X = playerCircle.Center.X - x,
                Y = playerCircle.Center.Y + y - 0.5f,
                W = x * 2,
                H = 1,
            };
        }
        SDL.SetRenderDrawColor(renderer, 255, 128, 128, 255);
        SDL.RenderFillRects(renderer, circleScanlines, circleScanlines.Length);

        var playerPosition = _sim.GetSystem<Spatial2DSystem>().GetTypedState(_player).Position;
        var sdlPlayerPositionRect = new SDL.FRect { X = playerPosition.X - 2, Y = playerPosition.Y - 2, W = 4, H = 4 };
        SDL.SetRenderDrawColor(renderer, 127, 255, 127, 255);
        SDL.RenderFillRect(renderer, in sdlPlayerPositionRect);


        // SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
        // SDL.RenderDebugText(renderer, 10, 10, $"Elapsed Time: {elapsed:F3} seconds");

        SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
        foreach (var wall in _maze.GetWalls())
        {
            SDL.RenderLine(renderer, wall.Start.X, wall.Start.Y, wall.End.X, wall.End.Y);
        }

        SDL.RenderPresent(renderer);

        if (currentCounter - _lastTickCounter >= _tickInterval)
        {
            _sim.Tick();
            _lastTickCounter = currentCounter;
        }
    }
}
