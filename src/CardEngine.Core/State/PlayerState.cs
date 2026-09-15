using CardEngine.Core.Cards;

namespace CardEngine.Core.State;

public sealed record PlayerState(
    int Index,
    int Health,
    IReadOnlyList<CardDefinition> Hand,
    int CardsRemaining);
