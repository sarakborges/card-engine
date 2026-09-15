using CardEngine.Core.Match;

namespace CardEngine.Serialization;

public static class RulesJsonSerializer
{
    public static MatchRules Deserialize(string json)
    {
        var document = JsonSerialization.Deserialize<RulesDocument>(json, "rules");
        var rules = new MatchRules(
            document.StartingHandSize,
            document.MaximumHandSize,
            document.MaximumBoardSize,
            document.MaximumTurns);
        rules.Validate();
        return rules;
    }

    public static string Serialize(MatchRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        rules.Validate();
        return JsonSerialization.Serialize(new RulesDocument
        {
            StartingHandSize = rules.StartingHandSize,
            MaximumHandSize = rules.MaximumHandSize,
            MaximumBoardSize = rules.MaximumBoardSize,
            MaximumTurns = rules.MaximumTurns,
        });
    }

    private sealed class RulesDocument
    {
        public int StartingHandSize { get; init; } = 3;
        public int MaximumHandSize { get; init; } = 10;
        public int MaximumBoardSize { get; init; } = 7;
        public int MaximumTurns { get; init; } = 100;
    }
}
