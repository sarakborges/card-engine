using CardEngine.Core.Content;
using CardEngine.Core.Match;

namespace CardEngine.Serialization;

public sealed record GameData(GameContent Content, MatchRules Rules);
