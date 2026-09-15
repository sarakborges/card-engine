using CardEngine.Core.Content;

namespace CardEngine.Core.Actions;

public abstract record MatchAction
{
    internal MatchAction()
    {
    }
}

public sealed record PlayCardAction(CardInstanceId CardInstanceId) : MatchAction;

public sealed record UseHeroPowerAction : MatchAction;

public sealed record EndTurnAction : MatchAction;
