using CardEngine.Core.Cards;

namespace CardEngine.Core.Game;

public sealed record GameConfig(
    IReadOnlyList<CardDefinition> Deck,
    int StartingHealth = 20,
    int OpeningHandSize = 3,
    int MaxTurns = 100);
