using SDL3;

Console.WriteLine("Initializing...");

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

if (!SDL.CreateWindowAndRenderer("Maze", MazeLevel.WindowPixels, MazeLevel.WindowPixels, 0, out var window, out var renderer))
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
    return;
}

var level = new MazeLevel();
var playerQuit = false;

Console.WriteLine("Let's go!");

while (!playerQuit)
{
    while (SDL.PollEvent(out var e))
    {
        if (level.HandleEvent(e))
        {
            playerQuit = true;
        }
    }

    if (playerQuit)
    {
        break;
    }

    level.UpdateAndRender(renderer);
    if (level.IsGameEnded)
    {
        level = new MazeLevel();
    }
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();
