namespace CardEngine.Core.State;

public sealed record GameState(
    IReadOnlyList<PlayerState> Players,
    int ActivePlayerIndex,
    int TurnNumber,
    bool IsFinished,
    int? WinnerIndex);
