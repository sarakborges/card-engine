using CardEngine.Core.Actions;
using CardEngine.Core.State;

namespace CardEngine.AI;

public interface IAgent
{
    MatchAction ChooseAction(MatchView view, IReadOnlyList<MatchAction> legalActions);
}
