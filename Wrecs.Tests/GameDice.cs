namespace Wrecs.Tests;

public interface IGameDice
{
    int Roll();
}

public class GameDice(int Count) : IGameDice
{
    private const int Sides = 6;
    private readonly Random _random = new();

    /// <summary>
    /// Rolls N six-sided dice and returns the total.
    /// </summary>
    public int Roll() => _random.Next(1 * Count, Sides * Count + 1);
}
