using CardEngine.AI;
using CardEngine.Core.Actions;
using CardEngine.Core.Cards;
using CardEngine.Core.Game;

var strike = new CardDefinition("strike", "Strike", 3);
var heavyStrike = new CardDefinition("heavy-strike", "Heavy Strike", 5);
var jab = new CardDefinition("jab", "Jab", 2);

var config = new GameConfig(
    [strike, jab, strike, heavyStrike, jab, strike, heavyStrike, jab, strike, jab],
    StartingHealth: 20,
    OpeningHandSize: 3,
    MaxTurns: 30);

var game = Game.Create(config, seed: 483729);
IAgent[] agents = [new RandomAgent(1001), new RandomAgent(1002)];

while (!game.State.IsFinished)
{
    var state = game.State;
    var actions = game.GetLegalActions();
    var action = agents[state.ActivePlayerIndex].ChooseAction(state, actions);

    Console.WriteLine($"Turn {state.TurnNumber}: P{state.ActivePlayerIndex + 1} -> {Describe(action)}");
    game.Apply(action);
}

var finalState = game.State;
Console.WriteLine(finalState.WinnerIndex is int winner
    ? $"Winner: P{winner + 1}"
    : "Result: draw");

static string Describe(GameAction action) => action switch
{
    PlayCardAction play => $"play {play.CardId} on P{play.TargetPlayerIndex + 1}",
    PassAction => "pass",
    _ => action.GetType().Name,
};
