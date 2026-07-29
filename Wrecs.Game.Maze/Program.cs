using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;
using Wrecs;
using Wrecs.Systems;

Console.WriteLine("Initializing...");


const float PlayerSize = 10;
const float PlayerSpeed = 10;
const float PlayerSprintMultiplier = 8;
Vector2 PlayerStart = new(1, 1);
const float MazeScale = 40;
const int MazeCells = 2;
const float MazeSize = MazeCells * MazeScale;
const float GoalSize = 30f;

// Create random maze
var baseMaze = MazeGenerator.Generate(MazeCells, MazeCells);
var maze = new ScaledMaze(baseMaze, MazeScale);

// Setup Wrecs Sim
var sim = new Sim();
var isGameEnded = new StrongBox<bool>(false);
sim.AddSystems(new Spatial2DSystem(),
               new GameBoundsConstraint(MazeSize + 1, MazeSize + 1, PlayerSize),
               new MazeWallsUpdateResolver(maze.GetWalls()),
               new AlignedRectangleSystem(),
               new AlignedRectangleCollisionEventSystem(),
               new PlayerGoalCollisionHandler(),
               new EndGameLatchSystem(isGameEnded));
var player = new PlayerAgent(PlayerSpeed, PlayerSprintMultiplier);
var goal = new GoalEntity();
var goalPosition = maze.GoalPosition;
sim.InitEntities((player, [new Spatial2DSnapshot(PlayerStart), new AlignedRectangleSnapshot(new(PlayerStart, PlayerSize, PlayerSize))]),
                 (goal, [new Spatial2DSnapshot(goalPosition), new AlignedRectangleSnapshot(new(goalPosition, GoalSize, GoalSize))]));

// HACK: Switch to STA Single Threaded Apartment
#if WINDOWS
#pragma warning disable CA1416 // Validate platform compatibility
Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
#endif

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}

if (!SDL.CreateWindowAndRenderer("Maze", (int)MazeSize + 1, (int)MazeSize + 1, 0, out var window, out var renderer))
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
    return;
}

var playerQuit = false;
var startCounter = SDL.GetPerformanceCounter();
var lastTickCounter = startCounter;
var frequency = SDL.GetPerformanceFrequency();
var tickInterval = frequency / 30.0;

Console.WriteLine("Let's go!");

while (!playerQuit && !isGameEnded.Value)
{
    while (SDL.PollEvent(out var e))
    {
        var type = (SDL.EventType)e.Type;
        if (type == SDL.EventType.Quit)
        {
            playerQuit = true;
        }
        else if (type == SDL.EventType.KeyDown || type == SDL.EventType.KeyUp)
        {
            switch (e.Key.Key)
            {
                case SDL.Keycode.Q:
                case SDL.Keycode.Escape:
                    playerQuit = true;
                    break;
                case SDL.Keycode.C:
                    sim.DisableSystem<MazeWallsUpdateResolver>();
                    break;
                case SDL.Keycode.K:
                    sim.EnableSystem<MazeWallsUpdateResolver>();
                    break;
            }
        }
    }
    player.HandleKeyboard();


    // Calculate elapsed time
    var currentCounter = SDL.GetPerformanceCounter();
    var elapsed = (currentCounter - startCounter) / (double)frequency;

    // Clear
    SDL.SetRenderDrawColor(renderer, 100, 149, 237, 255);
    SDL.RenderClear(renderer);

    // Draw maze walls
    SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
    foreach (var wall in maze.GetWalls())
    {
        SDL.RenderLine(renderer, wall.Start.X, wall.Start.Y, wall.End.X, wall.End.Y);
    }

    // Draw the goal
    var goalRect = sim.GetSystem<AlignedRectangleSystem>().GetTypedState(goal).Rectangle;
    var sdlGoalRect = new SDL.FRect
    {
        X = goalRect.BottomLeft.X,
        Y = goalRect.BottomLeft.Y,
        W = goalRect.Width,
        H = goalRect.Height,
    };
    SDL.SetRenderDrawColor(renderer, 255, 215, 0, 255);
    SDL.RenderFillRect(renderer, in sdlGoalRect);

    // Draw player rect
    var playerRect = sim.GetSystem<AlignedRectangleSystem>().GetTypedState(player).Rectangle;
    var sdlPlayerRect = new SDL.FRect { X = playerRect.BottomLeft.X, Y = playerRect.BottomLeft.Y, W = playerRect.Width, H = playerRect.Height };
    SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
    SDL.RenderFillRect(renderer, in sdlPlayerRect);

    // Draw player position
    var playerPosition = sim.GetSystem<Spatial2DSystem>().GetTypedState(player).Position;
    var sdlPlayerPositionRect = new SDL.FRect { X = playerPosition.X - 2, Y = playerPosition.Y - 2, W = 4, H = 4 };
    SDL.SetRenderDrawColor(renderer, 127, 255, 127, 255);
    SDL.RenderFillRect(renderer, in sdlPlayerPositionRect);

    // Draw overlay
    SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
    SDL.RenderDebugText(renderer, 10, 10, $"Elapsed Time: {elapsed:F3} seconds");

    SDL.RenderPresent(renderer);

    if (currentCounter - lastTickCounter >= tickInterval)
    {
        sim.Tick();
        lastTickCounter = currentCounter;
    }
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();
