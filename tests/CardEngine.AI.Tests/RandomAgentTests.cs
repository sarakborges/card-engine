using CardEngine.Core.Actions;
using CardEngine.Core.State;

namespace CardEngine.AI.Tests;

public sealed class RandomAgentTests
{
    [Fact]
    public void Equal_seeds_choose_the_same_action()
    {
        var state = new GameState([], 0, 1, false, null);
        GameAction[] actions = [new PassAction(), new PlayCardAction("strike", 1)];
        var first = new RandomAgent(123);
        var second = new RandomAgent(123);

        Assert.Equal(
            first.ChooseAction(state, actions),
            second.ChooseAction(state, actions));
    }
}
