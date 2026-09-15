namespace CardEngine.Core.Random;

public sealed class DeterministicRandomSource : IRandomSource
{
    private ulong _state;

    public DeterministicRandomSource(ulong seed)
    {
        _state = seed;
    }

    public int Next(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        var bound = (uint)exclusiveMax;
        var threshold = unchecked((uint)(0 - bound)) % bound;

        while (true)
        {
            var value = NextUInt32();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (var index = items.Count - 1; index > 0; index--)
        {
            var swapIndex = Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }

    private uint NextUInt32()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return (uint)(value >> 32);
    }
}
