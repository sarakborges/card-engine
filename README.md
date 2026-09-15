# CardEngine

A headless, deterministic .NET platform library for building data-driven card games.

`CardEngine` owns reusable card-game mechanics. The game that consumes the library owns its authored content, presentation and game-specific tuning.

## Packages

- `CardEngine.Core` — match runtime, turns, heroes, hero powers, deck/hand/board/discard, card instances, card types, effects, actions, validation and deterministic RNG.
- `CardEngine.Serialization` — optional JSON adapter for host-authored game data.
- `CardEngine.AI` — optional AI agent contract and stock agents.
- `CardEngine.Runner` — repository-only headless sample; not a package.

See `PLATFORM_ARCHITECTURE.md` for the platform/consumer contract and `ARCHITECTURE.md` for the general code architecture canon.

## Host-authored game data

The library does not contain specific heroes or cards. A consuming project can supply them from any source. The optional JSON adapter accepts data shaped like:

```json
{
  "rules": {
    "startingHandSize": 3,
    "maximumHandSize": 10,
    "maximumBoardSize": 7,
    "maximumTurns": 100
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
```

The host then creates a match by referencing definitions by ID:

```csharp
var data = GameDataSerializer.Deserialize(json);
var setup = new MatchSetup(
    [
        new PlayerSetup(new HeroId("starter"), playerDeck),
        new PlayerSetup(new HeroId("starter"), aiDeck),
    ],
    data.Rules);

var match = Match.Create(data.Content, setup, seed: 483729);
```

UI and AI submit actions from `GetLegalActions()`. Only the match runtime mutates authoritative state.

## Development flow

Work branches start from `develop` and pull requests target `develop`.
