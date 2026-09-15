using CardEngine.Core.Actions;
using CardEngine.Core.State;

namespace CardEngine.AI;

public sealed class RandomAgent(int seed) : IAgent
{
    private readonly System.Random _random = new(seed);

    public GameAction ChooseAction(GameState state, IReadOnlyList<GameAction> legalActions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(legalActions);

        if (legalActions.Count == 0)
        {
            throw new InvalidOperationException("The agent cannot choose an action when none are legal.");
        }

        return legalActions[_random.Next(legalActions.Count)];
    }
}
