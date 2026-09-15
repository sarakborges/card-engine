using CardEngine.Core.Actions;
using CardEngine.Core.State;

namespace CardEngine.AI;

public interface IAgent
{
    GameAction ChooseAction(GameState state, IReadOnlyList<GameAction> legalActions);
}
