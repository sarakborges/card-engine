using System.Text.Json;
using System.Text.Json.Serialization;
using CardEngine.Core.Content;
using CardEngine.Core.Effects;
using CardEngine.Core.Match;

namespace CardEngine.Serialization;

public sealed record GameData(GameContent Content, MatchRules Rules);

public static class GameDataSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static GameData Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var document = JsonSerializer.Deserialize<GameDataDocument>(json, Options)
            ?? throw new JsonException("Game data JSON is empty.");

        var cardTypes = document.CardTypes.Select(type => new CardTypeDefinition(
            new CardTypeId(type.Id),
            type.DestinationAfterPlay));
        var cards = document.Cards.Select(card => new CardDefinition(
            new CardId(card.Id),
            new CardTypeId(card.TypeId),
            card.Effects.Select(ToEffect).ToArray()));
        var heroPowers = document.HeroPowers.Select(power => new HeroPowerDefinition(
            new HeroPowerId(power.Id),
            power.UsesPerTurn,
            power.Effects.Select(ToEffect).ToArray()));
        var heroes = document.Heroes.Select(hero => new HeroDefinition(
            new HeroId(hero.Id),
            hero.StartingHealth,
            string.IsNullOrWhiteSpace(hero.HeroPowerId) ? null : new HeroPowerId(hero.HeroPowerId)));

        var content = GameContent.Create(cardTypes, cards, heroes, heroPowers);
        var rules = new MatchRules(
            document.Rules.StartingHandSize,
            document.Rules.MaximumHandSize,
            document.Rules.MaximumBoardSize,
            document.Rules.MaximumTurns);
        rules.Validate();
        return new GameData(content, rules);
    }

    public static string Serialize(GameData gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        var content = gameData.Content;
        var document = new GameDataDocument
        {
            Rules = new MatchRulesDocument
            {
                StartingHandSize = gameData.Rules.StartingHandSize,
                MaximumHandSize = gameData.Rules.MaximumHandSize,
                MaximumBoardSize = gameData.Rules.MaximumBoardSize,
                MaximumTurns = gameData.Rules.MaximumTurns,
            },
            CardTypes = content.CardTypes.OrderBy(type => type.Id.Value, StringComparer.Ordinal).Select(type => new CardTypeDocument
            {
                Id = type.Id.Value,
                DestinationAfterPlay = type.DestinationAfterPlay,
            }).ToList(),
            Cards = content.Cards.OrderBy(card => card.Id.Value, StringComparer.Ordinal).Select(card => new CardDocument
            {
                Id = card.Id.Value,
                TypeId = card.TypeId.Value,
                Effects = card.Effects.Select(ToDocument).ToList(),
            }).ToList(),
            HeroPowers = content.HeroPowers.OrderBy(power => power.Id.Value, StringComparer.Ordinal).Select(power => new HeroPowerDocument
            {
                Id = power.Id.Value,
                UsesPerTurn = power.UsesPerTurn,
                Effects = power.Effects.Select(ToDocument).ToList(),
            }).ToList(),
            Heroes = content.Heroes.OrderBy(hero => hero.Id.Value, StringComparer.Ordinal).Select(hero => new HeroDocument
            {
                Id = hero.Id.Value,
                StartingHealth = hero.StartingHealth,
                HeroPowerId = hero.HeroPowerId?.Value,
            }).ToList(),
        };

        return JsonSerializer.Serialize(document, Options);
    }

    private static EffectDefinition ToEffect(EffectDocument effect) => effect.Type switch
    {
        "damageOpponentHero" => new DamageOpponentHeroEffect(PositiveAmount(effect)),
        "healFriendlyHero" => new HealFriendlyHeroEffect(PositiveAmount(effect)),
        _ => throw new JsonException($"Unknown effect type '{effect.Type}'."),
    };

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

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class GameDataDocument
    {
        public MatchRulesDocument Rules { get; init; } = new();
        public List<CardTypeDocument> CardTypes { get; init; } = [];
        public List<CardDocument> Cards { get; init; } = [];
        public List<HeroDocument> Heroes { get; init; } = [];
        public List<HeroPowerDocument> HeroPowers { get; init; } = [];
    }

    private sealed class MatchRulesDocument
    {
        public int StartingHandSize { get; init; } = 3;
        public int MaximumHandSize { get; init; } = 10;
        public int MaximumBoardSize { get; init; } = 7;
        public int MaximumTurns { get; init; } = 100;
    }

    private sealed class CardTypeDocument
    {
        public string Id { get; init; } = string.Empty;
        public CardDestination DestinationAfterPlay { get; init; }
    }

    private sealed class CardDocument
    {
        public string Id { get; init; } = string.Empty;
        public string TypeId { get; init; } = string.Empty;
        public List<EffectDocument> Effects { get; init; } = [];
    }

    private sealed class HeroDocument
    {
        public string Id { get; init; } = string.Empty;
        public int StartingHealth { get; init; }
        public string? HeroPowerId { get; init; }
    }

    private sealed class HeroPowerDocument
    {
        public string Id { get; init; } = string.Empty;
        public int UsesPerTurn { get; init; } = 1;
        public List<EffectDocument> Effects { get; init; } = [];
    }

    private sealed class EffectDocument
    {
        public string Type { get; init; } = string.Empty;
        public int Amount { get; init; }
    }
}
