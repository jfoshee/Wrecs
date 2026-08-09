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

var window = SDL.CreateWindow("Polygons",
                              PolygonsGame.WindowWidth,
                              PolygonsGame.WindowHeight,
                              0);
if (window == 0)
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window: {SDL.GetError()}");
    SDL.Quit();
    return;
}

try
{
    using var renderer = new PolygonsGpuRenderer(window,
                                                  PolygonsGame.WindowWidth,
                                                  PolygonsGame.WindowHeight);
    Console.WriteLine($"GPU backend: {renderer.DriverName}");

    var game = new PolygonsGame();
    var playerQuit = false;

    Console.WriteLine("Let's go!");

    while (!playerQuit)
    {
        while (SDL.PollEvent(out var e))
        {
            if (game.HandleEvent(e))
            {
                playerQuit = true;
            }
        }

        if (!playerQuit)
        {
            game.UpdateAndRender(renderer);
        }
    }
}
catch (Exception exception)
{
    SDL.LogError(SDL.LogCategory.Application, exception.Message);
}
finally
{
    SDL.DestroyWindow(window);
    SDL.Quit();
}
