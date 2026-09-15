using CardEngine.Core.Actions;
using CardEngine.Core.Random;
using CardEngine.Core.State;

namespace CardEngine.AI;

public sealed class RandomAgent(ulong seed) : IAgent
{
    private readonly IRandomSource _random = new DeterministicRandomSource(seed);

    public MatchAction ChooseAction(MatchView view, IReadOnlyList<MatchAction> legalActions)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(legalActions);

        if (legalActions.Count == 0)
        {
            throw new InvalidOperationException("The agent cannot choose from an empty action list.");
        }

        return legalActions[_random.Next(legalActions.Count)];
    }
}
