namespace CardEngine.Core.Actions;

public abstract record GameAction;

public sealed record PlayCardAction(string CardId, int TargetPlayerIndex) : GameAction;

public sealed record PassAction : GameAction;
