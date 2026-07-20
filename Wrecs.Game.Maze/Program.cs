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
float x = 10, y = 50;

Console.WriteLine("Let's go!");

while (loop)
{
    while (SDL.PollEvent(out var e))
    {
        var type = (SDL.EventType)e.Type;
        if (type == SDL.EventType.Quit)
        {
            loop = false;
        }
        else if (type == SDL.EventType.KeyDown || type == SDL.EventType.KeyUp)
        {
            if (e.Key.Key == SDL.Keycode.Escape)
            {
                loop = false;
            }
            if (!e.Key.Repeat)
            {
                HandleKey(e.Key.Key, e.Key.Mod, type == SDL.EventType.KeyDown);
            }
        }
    }

    // Calculate elapsed time
    var currentCounter = SDL.GetPerformanceCounter();
    var elapsed = (currentCounter - startCounter) / (double)frequency;

    // Clear
    SDL.SetRenderDrawColor(renderer, 100, 149, 237, 255);
    SDL.RenderClear(renderer);

    // Draw rect
    var rect = new SDL.FRect { X = x, Y = y, W = 100, H = 100 };
    SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
    SDL.RenderFillRect(renderer, in rect);

    // Draw overlay
    SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
    SDL.RenderDebugText(renderer, 10, 10, $"Elapsed Time: {elapsed:F3} seconds");

    SDL.RenderPresent(renderer);
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();

void HandleKey(SDL.Keycode keycode, SDL.Keymod mod, bool isPressed)
{
    if (isPressed)
    {
        switch (keycode)
        {
            case SDL.Keycode.Up:
            case SDL.Keycode.W:
                y -= 10;
                break;
            case SDL.Keycode.Down:
            case SDL.Keycode.S:
                y += 10;
                break;
            case SDL.Keycode.Left:
            case SDL.Keycode.A:
                x -= 10;
                break;
            case SDL.Keycode.Right:
            case SDL.Keycode.D:
                x += 10;
                break;
        }
    }
}
