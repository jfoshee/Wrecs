using SDL3;

Console.WriteLine("Initializing...");

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}

if (!SDL.CreateWindowAndRenderer("SDL3 Create Window", 800, 600, 0, out var window, out var renderer))
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
    return;
}


var loop = true;
var startCounter = SDL.GetPerformanceCounter();
var frequency = SDL.GetPerformanceFrequency();

while (loop)
{
    while (SDL.PollEvent(out var e))
    {
        if ((SDL.EventType)e.Type == SDL.EventType.Quit)
        {
            loop = false;
        }
    }

    // Calculate elapsed time
    var currentCounter = SDL.GetPerformanceCounter();
    var elapsed = (currentCounter - startCounter) / (double)frequency;

    SDL.SetRenderDrawColor(renderer, 100, 149, 237, 255);
    SDL.RenderClear(renderer);

    SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
    SDL.RenderDebugText(renderer, 10, 10, $"Elapsed Time: {elapsed:F3} seconds");

    SDL.RenderPresent(renderer);
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();
