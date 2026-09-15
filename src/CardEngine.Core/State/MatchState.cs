using CardEngine.Core.Content;

namespace CardEngine.Core.State;

public sealed record CardState(CardInstanceId InstanceId, CardId DefinitionId);

public sealed record HeroState(
    HeroId DefinitionId,
    int Health,
    int HeroPowerUsesThisTurn);

public sealed record PlayerState(
    int Index,
    HeroState Hero,
    int DeckCount,
    IReadOnlyList<CardState> Hand,
    IReadOnlyList<CardState> Board,
    int DiscardCount);

public sealed record MatchState(
    long Revision,
    int TurnNumber,
    int ActivePlayerIndex,
    bool IsFinished,
    int? WinnerIndex,
    IReadOnlyList<PlayerState> Players);
