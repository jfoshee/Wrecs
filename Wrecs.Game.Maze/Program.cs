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

if (!SDL.CreateWindowAndRenderer("SDL3 Create Window", 800, 600, 0, out var window, out var renderer))
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
    return;
}


var loop = true;
var startCounter = SDL.GetPerformanceCounter();
var frequency = SDL.GetPerformanceFrequency();

Console.WriteLine("Let's go!");

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

    var rect = new SDL.FRect { X = 100, Y = 100, W = 100, H = 100 };
    SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
    SDL.RenderFillRect(renderer, in rect);

    SDL.RenderPresent(renderer);
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();
