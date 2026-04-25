namespace CommerceSim.Core;

public interface IOutput
{
    void WriteLine(string message);
}

public class NullOutput : IOutput
{
    public void WriteLine(string message) { }
}