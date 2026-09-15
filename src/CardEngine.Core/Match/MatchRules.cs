namespace CardEngine.Core.Match;

public sealed record MatchRules(
    int StartingHandSize = 3,
    int MaximumHandSize = 10,
    int MaximumBoardSize = 7,
    int MaximumTurns = 100)
{
    public void Validate()
    {
        if (StartingHandSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartingHandSize));
        }

        if (MaximumHandSize <= 0 || StartingHandSize > MaximumHandSize)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumHandSize));
        }

        if (MaximumBoardSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumBoardSize));
        }

        if (MaximumTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumTurns));
        }
    }
}
