namespace CardEngine.Core.Random;

public sealed class SeededRandomSource(int seed) : IRandomSource
{
    private readonly System.Random _random = new(seed);

    public int Next(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        return _random.Next(exclusiveMax);
    }

    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
