namespace CardEngine.Core.Random;

public interface IRandomSource
{
    int Next(int exclusiveMax);

    void Shuffle<T>(IList<T> items);
}
