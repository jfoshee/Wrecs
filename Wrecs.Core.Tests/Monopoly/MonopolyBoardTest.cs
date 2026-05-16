namespace Wrecs.Core.Tests.Monopoly;

public class MonopolyBoardTest
{
    [Fact(DisplayName = "Board configuration maps positions to properties correctly")]
    public void BoardConfig_MapsPositionsCorrectly()
    {
        MonopolyBoard.GetPropertyAtPosition(1).Should().NotBeNull();
        MonopolyBoard.GetPropertyAtPosition(1)!.Name.Should().Be("Mediterranean Avenue");

        MonopolyBoard.GetPropertyAtPosition(3).Should().NotBeNull();
        MonopolyBoard.GetPropertyAtPosition(3)!.Name.Should().Be("Baltic Avenue");

        MonopolyBoard.GetPropertyAtPosition(0).Should().BeNull(); // GO space
        MonopolyBoard.GetPropertyAtPosition(10).Should().BeNull(); // Jail
    }
}
