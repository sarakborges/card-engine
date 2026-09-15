using CardEngine.AI;
using CardEngine.Core.Actions;
using CardEngine.Core.State;
using Xunit;

namespace CardEngine.AI.Tests;

public sealed class RandomAgentTests
{
    [Fact]
    public void Equal_seeds_choose_the_same_action()
    {
        var view = new MatchView(1, 1, 0, false, null, 0, []);
        MatchAction[] actions = [new EndTurnAction(), new UseHeroPowerAction()];
        var first = new RandomAgent(123);
        var second = new RandomAgent(123);

        Assert.Equal(
            first.ChooseAction(view, actions),
            second.ChooseAction(view, actions));
    }
}
