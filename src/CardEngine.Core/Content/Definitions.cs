using CardEngine.Core.Effects;

namespace CardEngine.Core.Content;

public enum CardDestination
{
    DiscardPile,
    Board,
}

public sealed record CardTypeDefinition(CardTypeId Id, CardDestination DestinationAfterPlay);

public sealed record CardDefinition(
    CardId Id,
    CardTypeId TypeId,
    IReadOnlyList<EffectDefinition> Effects);

public sealed record HeroDefinition(
    HeroId Id,
    int StartingHealth,
    HeroPowerId? HeroPowerId = null);

public sealed record HeroPowerDefinition(
    HeroPowerId Id,
    int UsesPerTurn,
    IReadOnlyList<EffectDefinition> Effects);
