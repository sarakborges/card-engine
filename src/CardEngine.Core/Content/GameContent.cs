using CardEngine.Core.Effects;

namespace CardEngine.Core.Content;

public sealed class GameContent
{
    private readonly Dictionary<CardId, CardDefinition> _cards;
    private readonly Dictionary<CardTypeId, CardTypeDefinition> _cardTypes;
    private readonly Dictionary<HeroId, HeroDefinition> _heroes;
    private readonly Dictionary<HeroPowerId, HeroPowerDefinition> _heroPowers;

    private GameContent(
        Dictionary<CardId, CardDefinition> cards,
        Dictionary<CardTypeId, CardTypeDefinition> cardTypes,
        Dictionary<HeroId, HeroDefinition> heroes,
        Dictionary<HeroPowerId, HeroPowerDefinition> heroPowers)
    {
        _cards = cards;
        _cardTypes = cardTypes;
        _heroes = heroes;
        _heroPowers = heroPowers;
    }

    public IReadOnlyCollection<CardDefinition> Cards => _cards.Values;
    public IReadOnlyCollection<CardTypeDefinition> CardTypes => _cardTypes.Values;
    public IReadOnlyCollection<HeroDefinition> Heroes => _heroes.Values;
    public IReadOnlyCollection<HeroPowerDefinition> HeroPowers => _heroPowers.Values;

    public CardDefinition GetCard(CardId id) =>
        _cards.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown card id '{id}'.");

    public CardTypeDefinition GetCardType(CardTypeId id) =>
        _cardTypes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown card type id '{id}'.");

    public HeroDefinition GetHero(HeroId id) =>
        _heroes.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown hero id '{id}'.");

    public HeroPowerDefinition GetHeroPower(HeroPowerId id) =>
        _heroPowers.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown hero power id '{id}'.");

    public bool ContainsCard(CardId id) => _cards.ContainsKey(id);
    public bool ContainsHero(HeroId id) => _heroes.ContainsKey(id);

    public static GameContent Create(
        IEnumerable<CardTypeDefinition> cardTypes,
        IEnumerable<CardDefinition> cards,
        IEnumerable<HeroDefinition> heroes,
        IEnumerable<HeroPowerDefinition>? heroPowers = null)
    {
        ArgumentNullException.ThrowIfNull(cardTypes);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(heroes);

        var cardTypeMap = ToUniqueMap(cardTypes, definition => definition.Id, "card type");
        var cardMap = ToUniqueMap(cards.Select(Freeze), definition => definition.Id, "card");
        var heroMap = ToUniqueMap(heroes, definition => definition.Id, "hero");
        var heroPowerMap = ToUniqueMap(
            (heroPowers ?? Array.Empty<HeroPowerDefinition>()).Select(Freeze),
            definition => definition.Id,
            "hero power");

        Validate(cardTypeMap, cardMap, heroMap, heroPowerMap);
        return new GameContent(cardMap, cardTypeMap, heroMap, heroPowerMap);
    }

    private static CardDefinition Freeze(CardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(definition.Effects);
        return definition with { Effects = Array.AsReadOnly(definition.Effects.ToArray()) };
    }

    private static HeroPowerDefinition Freeze(HeroPowerDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(definition.Effects);
        return definition with { Effects = Array.AsReadOnly(definition.Effects.ToArray()) };
    }

    private static Dictionary<TKey, TDefinition> ToUniqueMap<TKey, TDefinition>(
        IEnumerable<TDefinition> definitions,
        Func<TDefinition, TKey> keySelector,
        string kind)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TDefinition>();
        foreach (var definition in definitions)
        {
            if (!result.TryAdd(keySelector(definition), definition))
            {
                throw new ArgumentException($"Duplicate {kind} id '{keySelector(definition)}'.");
            }
        }

        return result;
    }

    private static void Validate(
        IReadOnlyDictionary<CardTypeId, CardTypeDefinition> cardTypes,
        IReadOnlyDictionary<CardId, CardDefinition> cards,
        IReadOnlyDictionary<HeroId, HeroDefinition> heroes,
        IReadOnlyDictionary<HeroPowerId, HeroPowerDefinition> heroPowers)
    {
        if (cardTypes.Count == 0)
        {
            throw new ArgumentException("At least one card type must be defined.");
        }

        if (heroes.Count == 0)
        {
            throw new ArgumentException("At least one hero must be defined.");
        }

        foreach (var card in cards.Values)
        {
            if (!cardTypes.ContainsKey(card.TypeId))
            {
                throw new ArgumentException($"Card '{card.Id}' references missing card type '{card.TypeId}'.");
            }

            ValidateEffects(card.Effects, $"card '{card.Id}'");
        }

        foreach (var hero in heroes.Values)
        {
            if (hero.StartingHealth <= 0)
            {
                throw new ArgumentException($"Hero '{hero.Id}' must have positive starting health.");
            }

            if (hero.HeroPowerId is { } powerId && !heroPowers.ContainsKey(powerId))
            {
                throw new ArgumentException($"Hero '{hero.Id}' references missing hero power '{powerId}'.");
            }
        }

        foreach (var heroPower in heroPowers.Values)
        {
            if (heroPower.UsesPerTurn <= 0)
            {
                throw new ArgumentException($"Hero power '{heroPower.Id}' must allow at least one use per turn.");
            }

            ValidateEffects(heroPower.Effects, $"hero power '{heroPower.Id}'");
        }
    }

    private static void ValidateEffects(IReadOnlyList<EffectDefinition> effects, string owner)
    {
        ArgumentNullException.ThrowIfNull(effects);
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case DamageOpponentHeroEffect damage when damage.Amount <= 0:
                    throw new ArgumentException($"{owner} has a damage effect with a non-positive amount.");
                case HealFriendlyHeroEffect heal when heal.Amount <= 0:
                    throw new ArgumentException($"{owner} has a heal effect with a non-positive amount.");
                case null:
                    throw new ArgumentException($"{owner} contains a null effect.");
            }
        }
    }
}
