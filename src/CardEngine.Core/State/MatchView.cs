namespace CardEngine.Core.State;

public sealed record PlayerView(
    int Index,
    HeroState Hero,
    int DeckCount,
    int HandCount,
    IReadOnlyList<CardState> VisibleHand,
    IReadOnlyList<CardState> Board,
    int DiscardCount);

public sealed record MatchView(
    long Revision,
    int TurnNumber,
    int ActivePlayerIndex,
    bool IsFinished,
    int? WinnerIndex,
    int ViewerPlayerIndex,
    IReadOnlyList<PlayerView> Players);
