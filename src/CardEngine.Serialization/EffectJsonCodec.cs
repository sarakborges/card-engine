using System.Text.Json;
using CardEngine.Core.Effects;

namespace CardEngine.Serialization;

internal static class EffectJsonCodec
{
    internal static IReadOnlyList<EffectDefinition> ToDefinitions(IEnumerable<EffectDocument> documents) =>
        documents.Select(ToDefinition).ToArray();

    internal static List<EffectDocument> ToDocuments(IEnumerable<EffectDefinition> effects) =>
        effects.Select(ToDocument).ToList();

    private static EffectDefinition ToDefinition(EffectDocument effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return effect.Type switch
        {
            "damageOpponentHero" => new DamageOpponentHeroEffect(PositiveAmount(effect)),
            "healFriendlyHero" => new HealFriendlyHeroEffect(PositiveAmount(effect)),
            _ => throw new JsonException($"Unknown effect type '{effect.Type}'."),
        };
    }

    private static int PositiveAmount(EffectDocument effect)
    {
        if (effect.Amount <= 0)
        {
            throw new JsonException($"Effect '{effect.Type}' requires a positive amount.");
        }

        return effect.Amount;
    }

    private static EffectDocument ToDocument(EffectDefinition effect) => effect switch
    {
        DamageOpponentHeroEffect damage => new EffectDocument
        {
            Type = "damageOpponentHero",
            Amount = damage.Amount,
        },
        HealFriendlyHeroEffect heal => new EffectDocument
        {
            Type = "healFriendlyHero",
            Amount = heal.Amount,
        },
        _ => throw new NotSupportedException($"Unsupported effect type '{effect.GetType().Name}'."),
    };
}

internal sealed class EffectDocument
{
    public string Type { get; init; } = string.Empty;
    public int Amount { get; init; }
}
