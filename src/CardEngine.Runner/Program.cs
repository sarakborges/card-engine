using CardEngine.AI;
using CardEngine.Core.Match;
using CardEngine.Serialization;

const string gameDataJson = """
{
  "rules": {
    "startingHandSize": 3,
    "maximumHandSize": 10,
    "maximumBoardSize": 7,
    "maximumTurns": 30
  },
  "cardTypes": [
    { "id": "spell", "destinationAfterPlay": "discardPile" },
    { "id": "unit", "destinationAfterPlay": "board" }
  ],
  "cards": [
    {
      "id": "strike",
      "typeId": "spell",
      "effects": [{ "type": "damageOpponentHero", "amount": 3 }]
    },
    {
      "id": "guard",
      "typeId": "unit",
      "effects": []
    }
  ],
  "heroPowers": [
    {
      "id": "ping",
      "usesPerTurn": 1,
      "effects": [{ "type": "damageOpponentHero", "amount": 1 }]
    }
  ],
  "heroes": [
    { "id": "starter", "startingHealth": 20, "heroPowerId": "ping" }
  ]
}
""";

var data = GameDataSerializer.Deserialize(gameDataJson);
var deck = Enumerable.Range(0, 10)
    .Select(index => new CardEngine.Core.Content.CardId(index % 3 == 0 ? "guard" : "strike"))
    .ToArray();
var setup = new MatchSetup(
    [
        new PlayerSetup(new("starter"), deck),
        new PlayerSetup(new("starter"), deck),
    ],
    data.Rules);
var match = Match.Create(data.Content, setup, seed: 483729);
IAgent[] agents = [new RandomAgent(1001), new RandomAgent(1002)];

while (!match.State.IsFinished)
{
    var state = match.State;
    var actions = match.GetLegalActions();
    var view = match.GetViewForPlayer(state.ActivePlayerIndex);
    var action = agents[state.ActivePlayerIndex].ChooseAction(view, actions);

    Console.WriteLine($"Turn {state.TurnNumber} rev {state.Revision}: P{state.ActivePlayerIndex + 1} -> {action}");
    match.Apply(action, expectedRevision: state.Revision);
}

var finalState = match.State;
Console.WriteLine(finalState.WinnerIndex is int winner
    ? $"Winner: P{winner + 1}"
    : "Result: draw");
