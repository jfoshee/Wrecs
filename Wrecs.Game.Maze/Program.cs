using SDL3;

Console.WriteLine("Hello, World!");

if (!SDL.CreateWindowAndRenderer("SDL3 Create Window", 800, 600, 0, out var window, out var renderer))
{
    SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
    return;
}

SDL.SetRenderDrawColor(renderer, 100, 149, 237, 255);

var loop = true;

while (loop)
{
    while (SDL.PollEvent(out var e))
    {
        if ((SDL.EventType)e.Type == SDL.EventType.Quit)
        {
            loop = false;
        }
    }

    SDL.RenderClear(renderer);
    SDL.RenderPresent(renderer);
}

SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);

SDL.Quit();
